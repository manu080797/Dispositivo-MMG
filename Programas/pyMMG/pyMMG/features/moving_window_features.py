import numpy as np
import scipy.signal as signal
import scipy.optimize as optimize
import scipy.linalg as linear_algebra
import pyentrp.entropy as pyent

# Default config
config = {
    "Window length [s]": 1.0,
    "Overlap [0;1]": 0.9,
}

########################################## Utils ########################################################
def _autoregressive_fit(x, model_order):
    lag = np.arange(0, model_order, 1)

    n = x.shape[0]
    max_lag = np.max(lag)
    
    autocorrelation = np.correlate(x,x,mode='full')[n-1:]/n
    A = linear_algebra.toeplitz(autocorrelation[lag])
    
    b = autocorrelation[1:max_lag+2]
    ar_coefficients = np.zeros(max_lag+2)
    ar_coefficients[0] = 1.0
    ar_coefficients[1:] = np.linalg.solve(A,b)
    
    err_variance = autocorrelation[0]-np.dot(ar_coefficients[1:],b)
    
    AIC = 2*model_order/n + np.log(np.sqrt(err_variance))
    MDL = model_order*np.log(n)/n + np.log(np.sqrt(err_variance))
    
    return ar_coefficients, err_variance, AIC, MDL
#########################################################################################################

#######################  Functions to calculate moving window features ###################################
#
#   INPUT:
#   One 1d numpy array and one float. The x array represents a temporal window with signal samples and the fs float specifies
#   the sampling frequency.
#
#   OUTPUT:
#   Float representing the calculated feature value at the temporal window represented by x.
#
#   NOTE:
#   The function name will be used for identification of the feature in pandas dataframes (column name)
#

# WARNING: useless if moving window is too short (shorter than decorrelation time)
def AutocorrelationFrequency(x, fs):
    n = x.shape[0]
    t = np.linspace(0.0, n/fs, n)
    
    k = np.nonzero(t < DecorrelationTime(x, fs))[0]
    autocorrelation = signal.correlate(x, x, mode='full')[n+1-k:n+k] * signal.windows.hamming(2*k-1)
    
    fs = 1.0/(t[1]-t[0])
    f,resp = signal.freqz(autocorrelation, 1.0, fs=fs)
    gain = np.abs(resp)
    return f[np.nonzero(gain == np.max(gain))[0]][0]
    
# WARNING: useless if moving window is too short (shorter than decorrelation time)
def DecorrelationTime(x, fs, order=1):
    n = x.shape[0]
    t = np.linspace(0.0, n/fs, n)

    correlation = np.correlate(x,x,mode='full')[n+1:]
    counter = 0
    for i in range(0,correlation.shape[0]-1):
        if correlation[i+1]*correlation[i] < 0.0:
            counter += 1
            if counter == order:
                return t[i]
    return t[-1]

# WARNING: useless if moving window is too short (shorter than decorrelation time)
def MinimumAICLag(x, fs):
    n = x.shape[0]
    t = np.linspace(0.0, n/fs, n)

    max_lag = x.shape[0]/4
    akaike_information_criterion = lambda p : _autoregressive_fit(x, int(p))[2]
    res = optimize.minimize_scalar(akaike_information_criterion, bounds=(1,max_lag), method='Bounded')
    aproximate_minimum_aic_lag = int(res.x)
    
    search_interval = 10
    model_orders = np.arange(np.max([1, aproximate_minimum_aic_lag-search_interval]), np.min([max_lag, aproximate_minimum_aic_lag+search_interval]), 1)
    aic_vec = []
    for p in model_orders:
        aic_vec.append(akaike_information_criterion(p))
    
    minimum_aic_lag = np.nonzero(aic_vec == np.min(aic_vec))[0][0]+aproximate_minimum_aic_lag-search_interval-1
    return t[minimum_aic_lag]-t[0]

#ref: https://ieeexplore.ieee.org/document/4237165
# WARNING: extremly slow, needs serious optimization
#@nb.jit(nb.float64[:,:](nb.float64[:], nb.int64, nb.float64, nb.float64), nopython=True, parallel=False)
def _fuzzy_distance(x, m, n, r):
    length = x.shape[0]
    
    D = np.zeros((length-m, length-m))
    for i in range(0, length-m):
        for j in range(0, length-m):
            if (j != i):
                xmi = x[i:i+m]
                xmj = x[j:j+m]
                xmi = xmi-np.mean(xmi)
                xmj = xmj-np.mean(xmj)
                d = np.max(np.abs(xmi-xmj))
                D[i,j] = np.exp(-(d**n)/r)
            else:
                D[i,j] = 0.0
    return D

#ref: https://ieeexplore.ieee.org/document/4237165
# WARNING: extremly slow, needs serious optimization
#@nb.jit(nb.float64(nb.float64[:], nb.float64[:], nb.int64, nb.float64, nb.float64), nopython=True, parallel=False)
def FuzzyEntropy(x, fs, m=2, n=2.0, r_factor=0.2):
    r = np.std(x)*r_factor
    
    D0 = _fuzzy_distance(x,m,n,r)
    phi_0 = np.mean(D0)

    # Similar for m+1
    D1 = _fuzzy_distance(x,m+1,n,r)
    phi_1 = np.mean(D1)
    
    # Return FuzzyEn
    return float(np.log(phi_0)-np.log(phi_1))

def ShannonEntropy(x, fs):
    return pyent.shannon_entropy(x)

def PowerShannonEntropy(x, fs):
    return pyent.shannon_entropy(x**2)
    
def PermutationEntropy(x, fs, order=4, delay=7, normalize=True):
    return pyent.permutation_entropy(x, order=order, normalize=normalize, delay=delay)

# WARNING: possible NANs in output and very slow
def SampleEntropy(x, fs, m=2, r_factor=0.2):
    r = x.std()*r_factor
    return pyent.sample_entropy(x, m+1, r)[2]

def ZeroCrossing(x, fs):
    sign_changes = x[1:]*x[:-1]
    sign_changes = sign_changes[np.nonzero(sign_changes < 0.0)]
    return sign_changes.shape[0]/x.shape[0]*fs

def NumberOfTurns(x, fs):
    diff = x[1:]-x[:-1]
    sign_changes = diff[1:]*diff[:-1]
    sign_changes = sign_changes[np.nonzero(sign_changes < 0.0)]
    return sign_changes.shape[0]/x.shape[0]*fs

#ref: Robust, ultra low-cost MMG system with brain-machine-interface applications
# modified to always be positive by adding +1 in the log
def MMGScore(x, fs):
    return np.median(np.log(np.abs(x)+1))

def RMS(x, fs):
    return np.std(x)

def MedianPower(x, fs):
    return np.median(x**2)

def FrequencyMeanMW(x, fs):
    pxx = np.abs(np.fft.rfft(x))
    f = np.linspace(0,fs/2,pxx.shape[0])
    return np.trapz(pxx*f, f) / np.trapz(pxx, f)

def FrequencyVarianceMW(x, fs):
    pxx = np.abs(np.fft.rfft(x))
    f = np.linspace(0,fs/2,pxx.shape[0])
    mean = np.trapz(pxx*f, f) / np.trapz(pxx, f)
    return np.trapz(pxx*(f-mean*np.ones(pxx.shape[0]))**2, f)/np.trapz(pxx, f)

def HjorthMobility(x, fs):
    dx = np.gradient(x, 1.0/fs)
    dx_std = np.std(dx)
    x_std = np.std(x)
    return np.nan_to_num(dx_std / x_std, nan=0.0, posinf=0.0, neginf=0.0)

def HjorthComplexity(x, fs):
    dx = np.gradient(x, 1.0/fs)
    return np.nan_to_num(HjorthMobility(dx, fs) / HjorthMobility(x, fs), nan=0.0, posinf=0.0, neginf=0.0)
#########################################################################################################