import numpy as np
import pywt as pywt
import pandas as pd

from .features import distribution_features as d_features
from .features import moving_window_features as mw_features
from .features import instantaneous_features as i_features
from . import superlet

# default features to calculate (all available - very slow)
_instantaneous_features_list = [i_features.Variance,
                                i_features.AbsoluteValue,
                                i_features.PeakEnvelope,
                                i_features.PeakIntensity,
                                i_features.InstantaneousAmplitude,
                                i_features.InstantaneousFrequency,
                                i_features.EnvelopePeakEnvelope,
                                i_features.EnvelopePeakIntensity,
                                i_features.MeanOfAmplitude]

_moving_window_features_list = [mw_features.PermutationEntropy,
                                mw_features.AutocorrelationFrequency,
                                mw_features.DecorrelationTime,
                                mw_features.MinimumAICLag,
                                mw_features.FuzzyEntropy,
                                mw_features.ShannonEntropy,
                                mw_features.PowerShannonEntropy,
                                mw_features.PermutationEntropy,
                                mw_features.SampleEntropy,
                                mw_features.ZeroCrossing,
                                mw_features.NumberOfTurns,
                                mw_features.MMGScore,
                                mw_features.RMS,
                                mw_features.MedianPower,
                                mw_features.FrequencyMeanMW,
                                mw_features.FrequencyVarianceMW,
                                mw_features.HjorthComplexity,
                                mw_features.HjorthMobility]

_distribution_features_list = [d_features.Mode,
                               d_features.Mean,
                               d_features.Median,
                               d_features.Variance,
                               d_features.Skewness,
                               d_features.ExcessKurtosis,
                               d_features.Entropy,
                               d_features.Integral,
                               d_features.InterquantileRange50,
                               d_features.Quantile25,
                               d_features.Quantile75]

# default config
config = {"FrequencyBandDecompose enabled": True,
          "FrequencyBandDecompose wavelet": 'db10',
          "FrequencyBandDecompose initial_level": 1,
          "FrequencyBandDecompose max_level": 3,
          "Superlet minimum N cycles": 3,
          "Superlet max order": 15,
          "Superlet min order": 5,
          "Moving Window Features config": mw_features.config,
          "Instantaneous Features config": i_features.config,
          "Distribution Features config": d_features.config,
          "Instantaneous Features": _instantaneous_features_list,
          "Moving Window Features": _moving_window_features_list,
          "Distribution Features": _distribution_features_list}

# Calculates signal energy density using adaptive superlet transform (ASLT)
def SuperletPower(series, fs):
    if not isinstance(series, pd.Series):
        raise ValueError("ERROR: SuperletPower() needs a Pandas Series as series argument.")

    # set frequencies to calculate signal energy density
    n_frequencies = int(fs)
    f_min =  fs/2/1000
    f_max = fs/2
    f = np.linspace(f_min, f_max, n_frequencies)

    # refactor shape 
    buffer = np.zeros([1,series.shape[0]])
    buffer[0,:] = series.values

    # perform ASLT transform (returns energy density for each time sample and frequency in f array)
    p = superlet.aslt(buffer, fs, f, Ncyc=config["Superlet minimum N cycles"], ord=(config["Superlet min order"], config["Superlet max order"]))
    return f, p

# Decomposes signal in frequency bands using Wavelet Packet tranform
def FrequencyBandDecompose(dataframe):
    if not isinstance(dataframe, pd.DataFrame):
        raise ValueError("ERROR: FrequencyBandDecompose() needs Pandas DataFrame as dataframe argument.")

    # get MMG signal from dataframe
    mmg = dataframe["MMG"].values

    # read configuration
    wavelet = config["FrequencyBandDecompose wavelet"]
    max_level = config["FrequencyBandDecompose max_level"]
    initial_level = config["FrequencyBandDecompose initial_level"]

    # initializate Wavelet Packet tree 
    wavelet_packet = pywt.WaveletPacket(mmg, wavelet, mode='smooth')

    if (max_level is None):
      max_level = wavelet_packet.maxlevel
    else:
      if (max_level > wavelet_packet.maxlevel):
        raise Exception("Level"+str(max_level)+" is greater than maximum level "+str(wavelet_packet.maxlevel)+", which depends in signal length and wavelet order.")
     
    # calculate Wavelet Packet tree coefficients up to max level
    bands = {}
    for n in range(initial_level, max_level+1):
        _ = wavelet_packet.get_level(n)
        for node in wavelet_packet.get_leaf_nodes():
            # exclude path=='' which corresponds to level 0 (original signal)
            if (node.path != ''):
                label = "Banda "+str(node.path)
                # generate empty Wavelet Packet tree
                band = pywt.WaveletPacket(data=np.zeros(mmg.shape[0]), wavelet=wavelet)
                # fill Wavelet Packet tree with band coefficientes
                band[node.path] = wavelet_packet[node.path]
                # reconstruct band signal from filled Wavelet Packet tree
                bands[label] = band.reconstruct(update=False)

    # create dataframe with all frequency bands
    bands = pd.DataFrame(data=bands, index=dataframe.index)

    return bands

def InstantaneousFeatures(dataframe, instantaneous_features_list, fs):
    if not isinstance(dataframe, pd.DataFrame):
        raise ValueError("ERROR: InstantaneousFeatures() needs Pandas DataFrame as dataframe argument.")

    # Calculate every feature in instantaneous_features_list over each data column in dataframe
    features = {}
    for col in dataframe.drop(columns="Test ID").columns:
        N = dataframe[col].values.shape[0]
        time = np.linspace(0.0, N/fs, N)
        for f in instantaneous_features_list:
            label = col + " " + f.__name__ if f.__name__ != "<lambda>" else col + " lambda_IF_" + str(len(features))
            features[label] = f(time, dataframe[col].values)

    return pd.DataFrame(data=features, index=dataframe.index)

def MovingWindowFeatures(dataframe, moving_window_features_list, fs):
    if not isinstance(dataframe, pd.DataFrame):
        raise ValueError("ERROR: MovingWindowFeatures() needs Pandas DataFrame as dataframe argument.")

    # Calculate every feature in moving_window_features_list over each data column in dataframe
    features = {}
    for col in dataframe.drop(columns="Test ID").columns:
        for f in moving_window_features_list:
            label = col + " " + f.__name__ if f.__name__ != "<lambda>" else col + " lambda_MWF_" + str(len(features))
            features[label] = Rolling(dataframe[col], f, fs)

    return pd.DataFrame(data=features, index=dataframe.index)

# Applies function argument to a moving temporal window over series argument.
# Returns the interpolated feature value of each window to match series length.
def Rolling(series, function, fs):
    if not isinstance(series, pd.Series):
        raise ValueError("ERROR: Rolling() needs a Pandas Series as series argument.")

    elements_per_window = int(mw_features.config["Window length [s]"]*fs)
    overlap =  mw_features.config["Overlap [0;1]"]

    N = series.values.shape[0]
    step = int(elements_per_window*(1-overlap))
    windows_indexes = (np.expand_dims(np.arange(elements_per_window), 0) + np.expand_dims(np.arange(N-elements_per_window, step=step), 0).T)
    
    windows = series.values[windows_indexes]
    n_windows = windows.shape[0]

    window_index = np.linspace(int(elements_per_window/2), int(N-elements_per_window/2), n_windows)
    window_value = np.apply_along_axis(function, 1, windows, fs)
    interpolated_window_values = np.interp(np.linspace(0,N,N), window_index, window_value)
    return interpolated_window_values


def DistributionFeatures(dataframe, distribution_features_list, fs):
    if not isinstance(dataframe, pd.DataFrame):
        raise ValueError("ERROR: DistributionFeatures() needs Pandas DataFrame as dataframe argument.")

    features = {}

    frequency, power = SuperletPower(dataframe["MMG"], fs)
    n = power.shape[1]
    
    for f in distribution_features_list:
        label = "MMG Frequency " + f.__name__ if f.__name__ != "<lambda>" else "MMG lambda_DF_" + str(len(features))
        features[label] = [f(frequency, power[:,i]) for i in range(0, n)]
        
        # Add a low-pass filtered version of the distribution feature to feature dataframe
        label_ma = label + " (Avg)"
        features[label_ma] = d_features.LowPassFilter(features[label])
        
    return pd.DataFrame(data=features, index=dataframe.index)
    
def CalculateFeatures(data, fs):
    metadata_columns = ["Subject", "Test type", "Test metadata"]
    non_mmg_data_columns = ["Time", "Force"]

    # get functions to calculate each feature
    instantaneous_features_list = config["Instantaneous Features"]
    moving_window_features_list = config["Moving Window Features"]
    distribution_features_list = config["Distribution Features"]

    # decompose signal in frequency bands using Wavelet Packet Transform
    if config["FrequencyBandDecompose enabled"]:
        df_f_band_decomposition = data.groupby("Test ID").apply(FrequencyBandDecompose)
        data = data.join(df_f_band_decomposition)
        
    # calculate features in all frequency bands and original signal
    features = data.copy()
    if len(instantaneous_features_list) > 0:
        df_instantaneous_features = data.drop(columns=non_mmg_data_columns).drop(columns=metadata_columns).groupby("Test ID").apply(lambda df : InstantaneousFeatures(df, instantaneous_features_list, fs))
        features = features.join(df_instantaneous_features)
    if len(moving_window_features_list) > 0:
        df_moving_window_features = data.drop(columns=non_mmg_data_columns).drop(columns=metadata_columns).groupby("Test ID").apply(lambda df : MovingWindowFeatures(df, moving_window_features_list, fs))
        features = features.join(df_moving_window_features)
    if len(distribution_features_list) > 0:
        df_distribution_features = data.drop(columns=non_mmg_data_columns).drop(columns=metadata_columns).groupby("Test ID").apply(lambda df : DistributionFeatures(df, distribution_features_list, fs))
        features = features.join(df_distribution_features)

    # return dataframe with metadata, force, original signal, frequency decomposed signal, and signal features
    return features.interpolate(axis=0) # interpolate missing data and NANs

