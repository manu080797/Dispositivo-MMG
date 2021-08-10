import numpy as np
import pandas as pd
import scipy.stats as statistics
import itertools as it

# Get all columns with features from dataframe
def GetFeatures(dataframe):
    default_non_feature_columns = ["Subject", "Test type", "Test metadata", "MMG", "Force", "Time", "Test ID", "Target"]

    non_feature_columns = []
    for col in dataframe.columns:
        if "Banda" in col:
            if len(col.split(" ")) == 2:
                non_feature_columns.append(col)
        if col in default_non_feature_columns:
            non_feature_columns.append(col)

    return dataframe.drop(columns=non_feature_columns)

# Get all columns names with features from dataframe
def GetFeaturesColumnsNames(dataframe):
    return GetFeatures(dataframe).columns

# Calculates normalization constants for features, using initialMVC tests (mean feature value @ F > 0.9*MVC)
def CalculateNormalizationConstants(initialMVC_features, threshold = 0.9):
    features_columns_names = GetFeaturesColumnsNames(initialMVC_features)

    normalization_constants = {}
    for grp in initialMVC_features.groupby(by="Subject"):
        subject = grp[0]
        normalization_constants[subject] = {}
        normalization_constants[subject]["Force"] = grp[1]["Force"].max()

        max_force_index = np.nonzero(grp[1]["Force"].values > normalization_constants[subject]["Force"]*threshold)
        for feature in features_columns_names:
            normalization_constants[subject][feature] = np.mean(grp[1][feature].values[max_force_index])

    return pd.DataFrame(normalization_constants)

# Test if two trials of same subject at different % of MVC have statistically different mean feature value
def SameTargetTest(features, threshold = 1e-3):
    features_columns_names = GetFeaturesColumnsNames(features)

    rejected_percentage = {}

    targets = []
    for grp in features.groupby(by="Target"):
        targets.append(grp[0])
    targets.sort()

    for feature in features_columns_names:
        n_rejected_hypothesis = 0
        n_tests = 0
        for i in range(1,len(targets)-1):
            n_tests += 1
            a = features.loc[features["Target"] == targets[i-1], feature].values
            b = features.loc[features["Target"] == targets[i+1], feature].values
            _,pvalue = statistics.ttest_ind(a, b, equal_var=False)
            if pvalue > threshold:
                n_rejected_hypothesis += 1

        rejected_percentage[feature] = n_rejected_hypothesis/n_tests

    return rejected_percentage

# Test if two trials at same % of MVC and different subject have statistically different mean feature value
def SameSubjectTest(features, threshold = 1e-3):
    features_columns_names = GetFeaturesColumnsNames(features)

    n_tests = 0
    rejected_percentage = {}
    for feature in features_columns_names:
        rejected_percentage[feature] = 0

    for same_target in features.groupby(by="Target"):
        n_tests += 1
        for feature in features_columns_names:
            same_subject = [grp[1][feature].values for grp in same_target[1].groupby(by="Subject")]
            pvalue = []
            for c in it.combinations(range(len(same_subject)),2):
                pvalue.append(statistics.ttest_ind(same_subject[c[0]], same_subject[c[1]], equal_var=False)[1])
            pvalue = np.max(np.array(pvalue))

            if pvalue > threshold:
                rejected_percentage[feature] = rejected_percentage[feature] + 1

    for feature in features_columns_names:
        rejected_percentage[feature] = rejected_percentage[feature] / n_tests

    return rejected_percentage

# Selects features with high correlation with absolute force (r > 0,8 and tau > 0,6)
def NonNormalizedSelection(features, threshold = 1e-3):
    features_columns_names = GetFeaturesColumnsNames(features)

    lambdas = {}
    pearson_r = {}
    kendall_tau = {}
    selected_features = {}

    x = features["Force"].values

    for feature in features_columns_names:
        if (np.any(np.nonzero(features[feature].values <= 0.0))):
            y = features[feature].values
            l = 1
            print("WARNING: Negative or zero values encountered in feature ",feature,". Ignoring Box-Cox transformation.")
        else:
            y,l = statistics.boxcox(features[feature].values)

        r,r_pvalue = statistics.pearsonr(x,y)
        tau,tau_pvalue = statistics.kendalltau(x,y)

        if ((tau_pvalue < threshold) and (r_pvalue < threshold)):
            if ((tau > 0.6) and (r > 0.8)):
                lambdas[feature] = l
                pearson_r[feature] = r
                kendall_tau[feature] = tau
                selected_features[feature] = feature

    return pd.DataFrame(data=[selected_features, lambdas, pearson_r, kendall_tau], index=["Feature","lambda", "r", "tau"]).T.reset_index(drop=True)
    
# Selects features with high correlation with normalized force (r > 0,8 and tau > 0,6)
def ForceNormalizedSelection(original_features, normalization_constants, threshold = 1e-3):
    features = original_features.copy()
    features_columns_names = GetFeaturesColumnsNames(features)

    average_lambdas = {}
    for feature in features_columns_names:
        if (np.any(np.nonzero(features[feature].values <= 0.0))):
            average_lambdas[feature] = 1
            print("WARNING: Negative or zero values encountered in feature ",feature,". Ignoring Box-Cox transformation.")
        else:
            average_lambdas[feature] = 0.0
            n_subjects = 0
            for grp in features.groupby(by="Subject"):
                average_lambdas[feature] += statistics.boxcox_normmax(grp[1][feature].values, method='mle')
                n_subjects += 1
            average_lambdas[feature] /= n_subjects
            features[feature] = statistics.boxcox(features[feature].values, average_lambdas[feature])

    welch_test_rejected_percentage = {}
    pearson_r = {}
    pearson_r_pvalue = {}
    kendall_tau = {}
    kendall_tau_pvalue = {}
    kendall_tau_t = {}
    kendall_tau_pvalue_t = {}
    for grp in features.groupby(by="Subject"):
            subject = grp[0]
            grp[1]["Force"] /= normalization_constants.loc["Force",subject]

            welch_test_rejected_percentage[subject] = SameTargetTest(grp[1])

            pearson_r[subject] = {}
            pearson_r_pvalue[subject] = {}
            kendall_tau[subject] = {}
            kendall_tau_pvalue[subject] = {}
            kendall_tau_t[subject] = {}
            kendall_tau_pvalue_t[subject] = {}
            
            x = grp[1]["Force"].values
            t = grp[1]["Target"].values
            for feature in features_columns_names:
                y = grp[1][feature].values

                r,r_pvalue = statistics.pearsonr(x,y)
                tau,tau_pvalue = statistics.kendalltau(x,y)
                tau_t,tau_pvalue_t = statistics.kendalltau(t,y)

                pearson_r[subject][feature] = r
                pearson_r_pvalue[subject][feature] = r_pvalue
                kendall_tau[subject][feature] = tau
                kendall_tau_pvalue[subject][feature] = tau_pvalue
                kendall_tau_t[subject][feature] = tau_t
                kendall_tau_pvalue_t[subject][feature] = tau_pvalue_t

    subjects = []
    for grp in features.groupby(by="Subject"):
            subjects.append(grp[0])

    selected_features = {}
    agg = lambda x : np.min(x)
    for feature in features_columns_names:
        r = agg([pearson_r[subject][feature] for subject in subjects])
        r_pvalue = 10**np.max([np.log10(pearson_r_pvalue[subject][feature]) for subject in subjects])
        tau = agg([kendall_tau[subject][feature] for subject in subjects])
        tau_pvalue = 10**np.max([np.log10(kendall_tau_pvalue[subject][feature]) for subject in subjects])
        tau_t = agg([kendall_tau_t[subject][feature] for subject in subjects])
        tau_t_pvalue = 10**np.max([np.log10(kendall_tau_pvalue_t[subject][feature]) for subject in subjects])
        difference_between_targets = np.min([welch_test_rejected_percentage[subject][feature] for subject in subjects])

        if ((tau_pvalue < threshold) and (r_pvalue < threshold) and (tau_t_pvalue < threshold)):
            if ((tau > 0.6) and (r > 0.8) and (tau_t > 0.6)):
                pearson_r[feature] = r
                kendall_tau[feature] = tau
                selected_features[feature] = {"Feature": feature, "lambda (avg)": average_lambdas[feature], "r (min)": r, "tau (min)": tau, "tau target (min)": tau_t, "% H0 same target rejected": difference_between_targets}
    
    return pd.DataFrame(selected_features).T.reset_index(drop=True)

# Selects normalized features with high correlation with normalized force (r > 0,8 and tau > 0,6)
def BiNormalizedSelection(original_features, normalization_constants, threshold = 1e-3):
    features = original_features.copy()
    features_columns_names = GetFeaturesColumnsNames(features)
    
    for feature in features_columns_names:
        for grp in features.groupby(by="Subject"):
            subject = grp[0]
            features.loc[features["Subject"] == subject, feature] /= normalization_constants.at[feature, subject]
    
    for grp in features.groupby(by="Subject"):
            subject = grp[0]
            features.loc[features["Subject"] == subject, "Force"] /= normalization_constants.at["Force", subject]
    
    lambdas = {}
    for feature in features_columns_names:
        if (np.any(np.nonzero(features[feature].values <= 0.0))):
            y = features[feature].values
            l = 1
            print("WARNING: Negative or zero values encountered in feature ",feature,". Ignoring Box-Cox transformation. \nValues:",y)
        else:
            y,l = statistics.boxcox(features[feature].values)
        features[feature] = y
        lambdas[feature] = l

    same_target_test_rejected_percentage = SameTargetTest(features)
    same_subject_test_rejected_percentage = SameSubjectTest(features)
    
    pearson_r = {}
    kendall_tau = {}
    kendall_tau_target = {}
    difference_between_subjects = {}
    difference_between_targets = {}
    boxcox_lambda = {}
    selected_features = {}

    x = features["Force"].values
    t = features["Target"].values
    for feature in features_columns_names:
        y = features[feature].values
        r,r_pvalue = statistics.pearsonr(x,y)
        tau,tau_pvalue = statistics.kendalltau(x,y)
        tau_t,tau_pvalue_t = statistics.kendalltau(t,y)

        if ((tau_pvalue < threshold) and (r_pvalue < threshold) and (tau_pvalue_t < threshold)):
            if ((tau > 0.6) and (r > 0.8) and (tau_t > 0.6)):
                pearson_r[feature] = r
                kendall_tau[feature] = tau
                kendall_tau_target[feature] = tau_t
                boxcox_lambda[feature] = lambdas[feature]
                difference_between_subjects[feature] = same_subject_test_rejected_percentage[feature]
                difference_between_targets[feature] = same_target_test_rejected_percentage[feature]
                selected_features[feature] = feature

    return pd.DataFrame(data=[selected_features, boxcox_lambda, pearson_r, kendall_tau, kendall_tau_target, difference_between_targets, difference_between_subjects],
     index=["Feature", "lambda", "r", "tau", "tau target", "% H0 same target rejected", "% H0 same subject rejected"]).T.reset_index(drop=True)