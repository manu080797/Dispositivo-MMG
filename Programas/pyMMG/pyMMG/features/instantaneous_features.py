import numpy as np
import scipy.signal as signal

# Default config
config = {
    "Low Pass Filter Cut-Off Frequency (normalized)": 0.025,
    "Low Pass Filter Order": 2,
    "Low Pass Filter Type": 'butter',
}

########################################## Utils ########################################################
lp_filter = signal.iirfilter(config["Low Pass Filter Order"],
                             config["Low Pass Filter Cut-Off Frequency (normalized)"],
                             btype='lowpass',
                             output='sos',
                             ftype=config["Low Pass Filter Type"],
                             fs=2.0)

def LowPassFilter(x):
    pad = 1000
    filtered_x = signal.sosfiltfilt(lp_filter, np.pad(x,(pad,pad),mode='reflect'))[pad:-pad]
    return np.clip(filtered_x, 1e-30, None) # all features should be positive, negative values are due to gibbs phenomena
#########################################################################################################

#######################  Functions to calculate instantaneous features ###################################
#
#   INPUT:
#   Two 1d numpy arrays with same length. The x array represents each signal sample and t array represents
#   the time of each sample.
#
#   OUTPUT:
#   1d numpy array representing the calculated feature value at each time sample.
#
#   NOTE:
#   The function name will be used for identification of the feature in pandas dataframes (column name)
#
def Variance(t, x):
    # assumes mean = 0
    return LowPassFilter(x**2)

def AbsoluteValue(t, x):
    return LowPassFilter(np.abs(x))

def MeanOfAmplitude(t, x):
    return LowPassFilter(np.abs(np.gradient(x,t)))

def PeakEnvelope(t, x):
    n = x.shape[0]
    dx = np.gradient(x, t)
    pk_intensity = np.zeros(n)
    for i in range(1,n):
        if (dx[i-1]*dx[i] < 0.0):
            pk_intensity[i] = np.abs((x[i]+x[i-1])/2.0)
    return LowPassFilter(pk_intensity)

def PeakIntensity(t, x):
    n = x.shape[0]
    dx = np.gradient(x, t)
    pk_intensity_t = [t[0]]
    pk_intensity = [np.abs(x[0])]
    for i in range(1,n):
        if (dx[i-1]*dx[i] < 0.0):
            pk_intensity.append(np.abs(x[i]))
            pk_intensity_t.append(t[i])
    pk_intensity_t.append(t[-1]+(t[1]-t[0]))
    pk_intensity.append(np.abs(x[-1]))
    return LowPassFilter(np.interp(t, pk_intensity_t, pk_intensity))

def InstantaneousAmplitude(t, x):
    return LowPassFilter(np.abs(signal.hilbert(x)))

def InstantaneousFrequency(t, x):
    fs = 1.0/(t[1]-t[0])
    analytical_signal = signal.hilbert(x)
    instantaneous_phase = LowPassFilter(np.unwrap(np.angle(analytical_signal)))
    instantaneous_frequency = np.gradient(instantaneous_phase) / (2.0 * np.pi) * fs
    return LowPassFilter(instantaneous_frequency)

def EnvelopePeakIntensity(t,x):
    return PeakIntensity(t, np.abs(signal.hilbert(x)))

def EnvelopePeakEnvelope(t,x):
    return PeakEnvelope(t, np.abs(signal.hilbert(x)))