from matplotlib import pyplot as plt
from matplotlib import colors as colors
import numpy as np
import scipy.signal as signal

from . import superlet

def PlotForceMMG(data, fs, figsize=(20,10)):
    N = data["MMG"].shape[0]
    t = np.linspace(0,N/fs,N)

    plt.figure(figsize=figsize)

    plt.subplot(211)
    plt.plot(t,data["MMG"],label="MMG")
    plt.ylabel("Tensión [mV]")
    plt.legend()

    plt.subplot(212)
    plt.plot(t,data["Force"],label="Force")
    plt.ylabel("Fuerza [kgf]")
    plt.xlabel("Tiempo [s]")
    plt.legend()

def Periodogram(x, fs, figsize=(20,10), window='blackmanharris', nfft=None):
    if nfft is None:
        nfft = x.shape[0]
    f,pxx = signal.periodogram(x, fs, scaling='spectrum', window=window, nfft=nfft)

    plt.figure(figsize=figsize)
    plt.plot(f,pxx)
    plt.ylabel("Potencia espectral [$mV^2$]")
    plt.xlabel("Frecuencia [Hz]")

def Spectrogram(x, fs, figsize=(20,10), scale_type='log', scale_limits=None, plot_3d=False, min_n_cycle=3, min_order=5, max_order=15):
    n_frequencies = int(fs)
    f_min =  fs/2/1000
    f_max = fs/2

    f = np.linspace(f_min, f_max, n_frequencies)
    buffer = np.zeros([1,x.shape[0]])
    buffer[0,:] = x
    p = superlet.aslt(buffer, fs, f, Ncyc=min_n_cycle, ord=(min_order, max_order))

    n = p.shape[1]
    t = np.linspace(0,n/fs,n)
    
    plt.figure(figsize=figsize)
    if plot_3d:
        ax = plt.axes(projection='3d')
    else:
        ax = plt.axes()

    if scale_type == 'log':
        if scale_limits is None:
            scale_limits = (np.quantile(p, 0.0001), np.max(p))
        p[np.nonzero(p < scale_limits[0])] = scale_limits[0] * 1.00000001
        p[np.nonzero(p > scale_limits[1])] = scale_limits[1] * 0.999999999
        color_normalization = colors.LogNorm(scale_limits[0], scale_limits[1])
    elif scale_type == 'linear':
        if scale_limits is None:
            scale_limits = (0.0, np.max(p))
        p[np.nonzero(p < scale_limits[0])] = scale_limits[0] * 1.00000001
        p[np.nonzero(p > scale_limits[1])] = scale_limits[1] * 0.999999999
        color_normalization = colors.Normalize(scale_limits[0], scale_limits[1])
    else:
        color_normalization = None

    if (plot_3d):
        T, F = np.meshgrid(t, f)
        if scale_type == 'log':
            p = np.log(p)
        im = ax.plot_surface(T, F, p, cmap='jet')
    else:
        im = ax.imshow(p, cmap='jet', extent=(t[0], t[-1], f[-1], f[0]), aspect='auto', norm=color_normalization)

    plt.colorbar(im, ax=ax, orientation='horizontal', label='Densidad de energía [$mV^2$]', fraction=0.07)
    plt.xlabel("Tiempo [s]")
    plt.ylabel("Frecuencia [Hz]")
    plt.grid()
    plt.show()