import sys
import copy
import scipy.signal as signal

# Load submodules
from . import feature_calculator
from . import feature_selection
from . import preprocessing
from . import superlet
from . import visualization

# Initializate configuration variables
def _initConfig():
    current_module = sys.modules[__name__]
    current_module.config["feature_calculator"] = current_module.feature_calculator.config

# Gets a copy of the current configuration
def GetCurrentConfig():
    current_module = sys.modules[__name__]
    return copy.deepcopy(current_module.config)

# Updates configuration with newConfig
def SetConfig(newConfig):
    # Get current config
    current_module = sys.modules[__name__]
    config = current_module.config

    # Check new config for invalid keys
    for key in newConfig:
        if key not in config:
            raise KeyError("Invalid key in new configuration: "+str(key))

        if key == "feature_calculator":
            for subkey in newConfig[key]:
                if subkey not in config[key]:
                    raise KeyError("Invalid key in new configuration (feature_calculator): "+str(subkey))
                
                if subkey == "Moving Window Features config":
                    for subsubkey in newConfig[key][subkey]:
                        if subsubkey not in config[key][subkey]:
                            raise KeyError("Invalid key in new configuration (Moving Window Features config): "+str(subsubkey))

                if subkey == "Instantaneous Features config":
                    for subsubkey in newConfig[key][subkey]:
                        if subsubkey not in config[key][subkey]:
                            raise KeyError("Invalid key in new configuration (Instantaneous Features config): "+str(subsubkey))

                if subkey == "Distribution Features config":
                    for subsubkey in newConfig[key][subkey]:
                        if subsubkey not in config[key][subkey]:
                            raise KeyError("Invalid key in new configuration (Distribution Features config): "+str(subsubkey))

    # Write new config
    current_module.config = newConfig
    current_module.feature_calculator.config = newConfig["feature_calculator"]
    current_module.feature_calculator.i_features.config = newConfig["feature_calculator"]["Instantaneous Features config"]
    current_module.feature_calculator.d_features.config = newConfig["feature_calculator"]["Distribution Features config"]
    current_module.feature_calculator.mw_features.config = newConfig["feature_calculator"]["Moving Window Features config"]

    # Update internal variables
    feature_config = current_module.feature_calculator.i_features.config
    current_module.feature_calculator.i_features.lp_filter = signal.iirfilter(feature_config["Low Pass Filter Order"],
                                                                              feature_config["Low Pass Filter Cut-Off Frequency (normalized)"],
                                                                              btype='lowpass',
                                                                              output='sos',
                                                                              fs=2.0,
                                                                              ftype=feature_config["Low Pass Filter Type"])

    feature_config = current_module.feature_calculator.d_features.config
    current_module.feature_calculator.d_features.lp_filter = signal.iirfilter(feature_config["Low Pass Filter Order"],
                                                                              feature_config["Low Pass Filter Cut-Off Frequency (normalized)"],
                                                                              btype='lowpass',
                                                                              output='sos',
                                                                              fs=2.0,
                                                                              ftype=feature_config["Low Pass Filter Type"])

# code executed when pyMMG module is loaded  
config = {}
_initConfig()
