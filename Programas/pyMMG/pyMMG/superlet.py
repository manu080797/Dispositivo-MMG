##################################################################################################################
#   ADAPTIVE SUPERRESOLUTION WAVELET (SUPERLET) TRANSFORM 
# 
#   ORIGINAL AUTHOR:         Harald Bârzan
#   PYTHON TRANSLATION:     Manuel Lima
#   DATE:           June 2021
#   DESCRIPTION:
#
#   Computes the adaptive superresolution wavelet (superlet) transform on 
#   input data to produce a time-frequency representation. For each 
#   frequency of interest, the closest integer order from the order 
#   interval will be chosen to produce each superlet. A superlet is a set 
#   of wavelets with the same center frequency but different number of 
#   cycles.
#
#   REFERENCE:
#   
#   Superlets: time-frequency super-resolution using wavelet sets
#   Moca, V.V., Nagy-Dãbâcan, A., Bârzan, H., Mure?an, R.C.
#   https://www.biorxiv.org/content/10.1101/583732v1.full
#   
#   NOTES:
#
#   If the input data consists of multiple buffers, a wavelet spectrum will
#   be computed for each of the buffers and averaged to produce the final 
#   result.
#   If the order parameter (ord) is empty, this function will return the
#   standard CWT (one wavelet per frequency of interest).
#
#   INPUT:
#   > input         - input data as numpy array with shape (n_buffers, n_samples)
#   > Fs            - sampling frequency in Hz
#   > F             - frequency-of-interest numpy array with shape (n_frequencies)
#   > Ncyc          - number of initial wavelet cycles (default: 3)
#   > ord           - two-element integer tuple (min_ord, max_ord) (default: None)
#   > mult          - specifies the use of multiplicative superresolution
#                     (False - additive, True - multiplicative)
#
#   OUTPUT:
#   > wtresult      - superlet spectrum as numpy array with shape (n_frequencies, n_samples)
##################################################################################################################
import numba as nb
import numpy as np

# compute the complex wavelet coefficients for the desired time point t, bandwidth bw and center frequency cf
@nb.jit(nb.complex128(nb.float64, nb.float64, nb.float64), nopython=True, fastmath=True)
def bw_cf(t, bw, cf):
    cnorm = 1.0 / (bw * np.sqrt(2 * np.pi))
    exp1 = cnorm * np.exp(-(t**2) / (2 * bw**2))
    return np.exp(2j * np.pi * cf * t) * exp1

# compute the gaussian coefficient for the desired time point t and standard deviation sd
@nb.jit(nb.float64(nb.float64, nb.float64), nopython=True, fastmath=True)
def gauss(t, sd):
    cnorm = 1 / (sd * np.sqrt(2 * np.pi))
    res = cnorm * np.exp(-(t ** 2) / (2 * sd ** 2))
    return res

# computes the complex Morlet wavelet for the desired center frequency Fc with Nc cycles, with a sampling frequency Fs.
@nb.jit(nb.complex128[:](nb.float64, nb.int64, nb.float64), nopython=True, fastmath=True)
def cxmorlet(Fc, Nc, Fs):
    #we want to have the last peak at 2.5 SD
    sd = (Nc / 2) * (1 / Fc) / 2.5
    wl = int(2 * np.floor((6 * sd * Fs) / 2) + 1)
    w = np.zeros(wl, dtype=np.complex128)
    gi = 0
    off = int(wl / 2)

    for i in range(0,wl):
        t = (i - 1 - off) / Fs
        w[i] = bw_cf(t, sd, Fc)
        gi = gi + gauss(t, sd)

    w = w / gi
    return w

def aslt(input, Fs, F, Ncyc, ord=None, mult=True):
    if (ord is None):
        order_ls = np.ones(F.shape[0], dtype=np.int64)
    else:
        order_ls = np.fix(np.linspace(ord[0], ord[1], F.shape[0])).astype(int)

    # get the input size
    Nbuffers, Npoints = input.shape

    padding = 0

    wavelets = [[None for o in range(0,np.max(ord))] for f in range(0,F.shape[0])]

    # initialize wavelet sets for either additive or multiplicative superresolution
    if (mult):
        for i_freq in range(0,F.shape[0]):
            for i_ord in range(0,order_ls[i_freq]):
                # each new wavelet has Ncyc extra cycles (multiplicative superresolution)
                wavelets[i_freq][i_ord] = cxmorlet(F[i_freq], Ncyc * i_ord + 1, Fs)

                # the margin will be the half-size of the largest wavelet
                padding = np.max([padding, int(wavelets[i_freq][i_ord].shape[0] / 2.0)])
    else:
        for i_freq in range(0,F.shape[0]):
            for i_ord in range(0,order_ls[i_freq]):
                # each new wavelet has an extra cycle (additive superresolution)
                wavelets[i_freq][i_ord] = cxmorlet(F[i_freq], Ncyc + i_ord + 1, Fs)

                # the margin will be the half-size of the largest wavelet
                padding = np.max([padding, int(wavelets[i_freq][i_ord].shape[0] / 2.0)])

    # the zero-padded buffer
    buffer = np.zeros(Npoints + 2 * padding)

    # the output scalogram
    wtresult = np.zeros([F.shape[0], Npoints])

    # convenience indexers for the zero-padded buffer
    bufbegin = padding
    bufend = padding + Npoints

    # loop over the input buffers
    for i_buf in range(0, Nbuffers):
        for i_freq in range(0, F.shape[0]):

            # pooling buffer, starts with 1 because we're doing geometric mean
            temp = np.ones(Npoints)

            # fill the central part of the buffer with input data
            buffer[bufbegin:bufend] = input[i_buf, :]

            # compute the convolution of the buffer with each wavelet in the current set
            for i_ord in nb.prange(0, order_ls[i_freq]):
                # restricted convolution (input size == output size)
                tempcx = np.convolve(buffer, wavelets[i_freq][i_ord], mode='same')

                # accumulate the magnitude (times 2 to get the full spectral energy
                temp = temp * (2 * np.abs(tempcx[bufbegin:bufend]) ** 2).T

            # compute the power of the geometric mean
            root = 1 / order_ls[i_freq]
            temp = temp ** root

            # accumulate the current FOI to the result spectrum
            wtresult[i_freq, :] = wtresult[i_freq, :] + temp

    # scale the output by the number of input buffers
    wtresult = wtresult / Nbuffers

    return wtresult
