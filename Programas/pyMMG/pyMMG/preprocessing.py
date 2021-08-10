import pywt as pywt
import numpy as np
import os as os
import pandas as pd
import scipy.signal  as sg

def WaveletThresholding(signal, noise_floor_measurement, wavelet="db10", threshold_function='soft'):
    level = int(np.min([pywt.dwt_max_level(signal.shape[0],wavelet),pywt.dwt_max_level(noise_floor_measurement.shape[0],wavelet)]))
    N = signal.shape[0]
    pad = 2**level

    noise_floor_measurement = pywt.pad(noise_floor_measurement,(pad,pad),mode='smooth')
    signal = pywt.pad(signal,(pad,pad),mode='smooth')
    
    noise_coefficients = pywt.wavedec(noise_floor_measurement, wavelet, mode="smooth", level=level)
    estimated_sigma = np.array([np.median(np.abs(coefficient-np.median(coefficient)))/0.6745 for coefficient in noise_coefficients[1:]])

    signal_coefficients = pywt.wavedec(signal, wavelet, mode="smooth", level=level)

    n = 1
    for s in estimated_sigma:
        if (n >= len(signal_coefficients)):
          break
        threshold = s*np.sqrt(2*np.log(N))
        signal_coefficients[n] = pywt.threshold(signal_coefficients[n], threshold, mode=threshold_function)
        n += 1

    reconstructed_signal = pywt.waverec(signal_coefficients, wavelet, mode="smooth")[pad:pad+N]
    
    return reconstructed_signal

def NormalizeForce(data, log=False):
    initialMVC = data.loc[data["Test type"] == "initialMVC"].groupby(by="Subject")["Force"].max()

    for grp in initialMVC.groupby(by="Subject"):
        subject = grp[0]
        max_force = grp[1].reset_index()["Force"].values
        if log: print("Subject:",subject," Max force:",max_force)
        data.loc[data["Subject"] == subject, "Force"] /= max_force

    return data

def CropForceTestToTargetForce(data, tolerance=0.05, n_points=2000):
    initialMVC = data.loc[data["Test type"] == "initialMVC"].groupby(by="Subject")["Force"].max()

    subject_mvc = {}
    for grp in initialMVC.groupby(by="Subject"):
        subject = grp[0]
        subject_mvc[subject] = grp[1].reset_index()["Force"].values

    cropped_data = {}
    for grp in data.groupby(by="Test ID"):
        test_id = grp[0]
        test_data = grp[1]

        if test_data["Test type"].iloc[0] == "force":
            subject_max_force = subject_mvc[test_data["Subject"].iloc[0]]
            target = float(test_data["Test metadata"].iloc[0][1]) / 100.0 * subject_max_force
            print("MVC:",subject_max_force,"Target:",target,"Bounds: (",target*(1-tolerance),";",target*(1+tolerance),")")
            valid_indexes = np.nonzero((test_data["Force"].values > target*(1-tolerance)) & (test_data["Force"].values < target*(1+tolerance)))[0]
            valid_indexes = np.arange(valid_indexes[-1]-n_points, valid_indexes[-1])
            cropped_data[test_id] = test_data.iloc[valid_indexes, :]
        else:
            cropped_data[test_id] = test_data

    return pd.concat(cropped_data).reset_index(drop=True)

def CropForceTestToInterquantileRange(force_test_data, range=0.5, n_points=None):
    cropped_data = {}
    for grp in force_test_data.groupby(by="Test ID"):
        test_id = grp[0]
        test_data = grp[1]

        if test_data["Test type"].iloc[0] == "force":
            low_bound = np.quantile(test_data["Force"].values, 0.5-range/2.0)
            high_bound = np.quantile(test_data["Force"].values, 0.5+range/2.0)
            valid_indexes = np.nonzero((test_data["Force"].values > low_bound) & (test_data["Force"].values < high_bound))[0]

            if (n_points is None):
                valid_indexes = np.arange(valid_indexes[0], valid_indexes[-1])
            else:
                if (valid_indexes[-1]-n_points < test_data["Force"].values.shape[0]):
                    valid_indexes = np.arange(valid_indexes[-1]-n_points, valid_indexes[-1])
                else:
                    valid_indexes = np.arange(valid_indexes[0], valid_indexes[-1])
    
            cropped_data[test_id] = test_data.iloc[valid_indexes, :]
        else:
            cropped_data[test_id] = test_data

    return pd.concat(cropped_data).reset_index(drop=True)

def CropEnduranceTimeTest(endurance_test_data, tolerance=0.05):
    cropped_data = {}
    for grp in endurance_test_data.groupby(by="Test ID"):
        test_id = grp[0]
        test_data = grp[1]

        if test_data["Test type"].iloc[0] == "endurancetime":
            target = np.median(test_data["Force"].values)
            valid_indexes = np.nonzero((test_data["Force"].values > target*(1-tolerance)) & (test_data["Force"].values < target*(1+tolerance)))[0]
            valid_indexes = np.arange(valid_indexes[0], valid_indexes[-1])
            cropped_data[test_id] = test_data.iloc[valid_indexes, :]
        else:
            cropped_data[test_id] = test_data

    return pd.concat(cropped_data).reset_index(drop=True)

def CSVToDataFrame(root, fs, fc_mmg=(25.0, None), fc_force=3.0, invert_channels=False, wavelet_thresholding=True, wavelet='db20', load_cell_calibration=(-0.2681, 544.7), log=True):
    tmp_list_of_dataframes = []

    # list all files in root folder
    files = os.listdir(root)

    # calculate IIR filter coeficients
    if fc_force is not None:
        force_filter = sg.iirfilter(8, fc_force, btype='lowpass', output='sos', fs=fs)
    else:
        force_filter = None
    if fc_mmg is not None:
        if fc_mmg[0] is not None:
            hp_mmg_filter = sg.iirfilter(8, fc_mmg[0], btype='highpass', output='sos', fs=fs)
        else:
            hp_mmg_filter = None
        if fc_mmg[1] is not None:
            lp_mmg_filter = sg.iirfilter(8, fc_mmg[1], btype='lowpass', output='sos', fs=fs)
        else:
            lp_mmg_filter = None
    else:
        hp_mmg_filter = None
        lp_mmg_filter = None

    # load noise measurement for wavelet thresholding
    noise_measurement = {}
    for filename in files:
        splitted_filename = filename.split(".")
        if (splitted_filename[-1] == "csv"):
            if ("noise" in splitted_filename[0]):
                # read noise data from csv
                filepath = os.path.normpath(root+filename)

                if invert_channels:
                    column_names = ("Index", "Force", "MMG")
                else:
                    column_names = ("Index", "MMG", "Force")

                noise_measurement[splitted_filename[0].split("_")[0]] = pd.read_csv(filepath, delimiter=";", decimal=".", skiprows=20, names=column_names)

    for filename in files:
        splitted_filename = filename.split(".")
        if (splitted_filename[-1] == "csv"):
            if ("noise" not in splitted_filename[0]):

                label = splitted_filename[0].split("_")

                if (len(label) >= 4):
                    subject = label[0]
                    test_type = label[1]
                    test_id = label[2]
                    test_metadata = label[3:]
                else:
                    test_id = label[0]
                    subject = "unknown"
                    test_type = "unknown"
                    test_metadata = "none"

                if log:
                    print("Reading file: ", filename)
                    print("Subject: ",subject)
                    print("Test type: ",test_type)
                    print("Test ID: ",test_id)
                    print("Test metadata",test_metadata,"\n")

                # read data from csv
                filepath = os.path.normpath(root+filename)

                if invert_channels:
                    column_names = ("Index", "Force", "MMG")
                else:
                    column_names = ("Index", "MMG", "Force")

                csv_data = pd.read_csv(filepath, delimiter=";", decimal=".", skiprows=20, names=column_names)

                # wavelet thresholding
                if wavelet_thresholding:
                    if subject in noise_measurement:
                        noise_data = noise_measurement[subject]
                    elif "global" in noise_measurement.keys():
                        noise_data = noise_measurement["global"]
                    else:
                        print("ERROR: no noise data found for subject and no global noise measurement available.")
                        return None
                        
                csv_data["MMG"] = WaveletThresholding(csv_data["MMG"].values, noise_data["MMG"].values, wavelet=wavelet)
                
                # IIR filtering
                if force_filter is not None:
                    csv_data["Force"] = sg.sosfiltfilt(force_filter, csv_data["Force"].values)
                if hp_mmg_filter is not None:
                    csv_data["MMG"] = sg.sosfiltfilt(hp_mmg_filter, csv_data["MMG"].values)
                if lp_mmg_filter is not None:  
                    csv_data["MMG"] = sg.sosfiltfilt(lp_mmg_filter, csv_data["MMG"].values)

                # generate force, mmg and time variables
                n = csv_data["MMG"].values.shape[0]
                mmg = csv_data["MMG"].values
                slope = load_cell_calibration[0]
                intercept = load_cell_calibration[1]
                force = slope*csv_data["Force"].values+intercept # load cell calibration (from linear regression)

                # write data to pandas DataFrame
                n = mmg.shape[0]
                time = np.linspace(0,n/fs,n)
                index = np.linspace(0,n,n)
                subject = [subject for i in range(n)]
                test_type = [test_type for i in range(n)]
                test_id = [test_id for i in range(n)]
                test_metadata = [test_metadata for i in range(n)]

                tmp_dict = {"Subject": subject,
                            "Test type":  test_type,
                            "Test ID": test_id,
                            "Test metadata": test_metadata,
                            "Time": time,
                            "Force": force,
                            "MMG": mmg}

                tmp_list_of_dataframes.append(pd.DataFrame(data=tmp_dict, index=index))

    return pd.concat(tmp_list_of_dataframes, ignore_index=True)

