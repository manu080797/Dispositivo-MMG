import numpy as np
import scipy.integrate as integrate
import scipy.stats as statistics
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

    # all features should be positive, negative values are due to gibbs phenomena from low-pass filter
    return np.clip(filtered_x, 1e-30, None)

def _quantile(x, dist, q):
    normalized_cdf = integrate.cumtrapz(dist, x)
    normalized_cdf /= np.max(normalized_cdf)
    return x[np.nonzero(normalized_cdf >= q)][0]
#########################################################################################################

#######################  Functions to calculate distribution features ###################################
#
#   INPUT:
#   Two 1d numpy arrays with same length. The x array represents posible values for a random
#   variable and the dist array represents the probability for such value. You can also
#   interpret the x array as frecuencies and the dist array as the spectral power for each
#   frequency.
#
#   OUTPUT:
#   Float representing the calculated feature value
#
#   NOTE:
#   The function name will be used for identification of the feature in pandas dataframes (column name)
#
def Mode(x, dist):
    return x[np.nonzero(dist == np.max(dist))][0]

def Median(x, dist):
    normalized_cdf = integrate.cumtrapz(dist, x)
    normalized_cdf /= np.max(normalized_cdf)
    return x[np.nonzero(normalized_cdf >= 0.5)][0]

def InterquantileRange50(x, dist):
    return _quantile(x,dist,0.75)-_quantile(x,dist,0.25)

def Quantile25(x, dist):
    return _quantile(x,dist,0.25)

def Quantile75(x, dist):
    return _quantile(x,dist,0.75)

def Mean(x, dist):
    return np.trapz(dist*x, x) / np.trapz(dist, x)
    
def Variance(x, dist):
    n = np.shape(x)[0]
    mean = Mean(x, dist)
    return np.trapz(dist*(x-mean*np.ones(n))**2, x)/np.trapz(dist, x)
    
def Skewness(x, dist):
    n = np.shape(x)[0]
    mean = Mean(x, dist)
    variance = Variance(x, dist)
    return np.trapz(dist*(x-mean*np.ones(n))**3, x)/np.trapz(dist, x)/variance**1.5

def ExcessKurtosis(x,dist):
    n = np.shape(x)[0]
    mean = Mean(x,dist)
    variance = Variance(x,dist)
    return np.trapz(dist*(x-mean*np.ones(n))**4, x)/np.trapz(dist, x)/variance**2.0-3.0

def Entropy(x,dist):
    return statistics.entropy(dist, base=x.shape[0])

def Integral(x,dist):
    return np.trapz(dist, x=x)
#########################################################################################################