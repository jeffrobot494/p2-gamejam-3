mergeInto(LibraryManager.library, {

    // Store references to Web Audio nodes for each AudioSource
    RadioFilters: {},

    // Apply radio filters to an AudioSource in WebGL
    ApplyRadioFiltersWebGL: function(audioSourceName, enableLowPass, lowPassCutoff, enableHighPass, highPassCutoff, enableDistortion, distortionAmount) {
        var name = UTF8ToString(audioSourceName);

        try {
            // Access Unity's Web Audio context
            if (typeof WEBAudio === 'undefined' || !WEBAudio.audioContext) {
                console.warn('[RadioFilters] Web Audio context not available yet');
                return;
            }

            var audioContext = WEBAudio.audioContext;

            // Find the Unity AudioSource by name
            // Unity's WebGL audio sources are stored in WEBAudio.audioInstances
            var audioSource = null;
            if (WEBAudio.audioInstances) {
                for (var i = 0; i < WEBAudio.audioInstances.length; i++) {
                    var instance = WEBAudio.audioInstances[i];
                    if (instance && instance.source) {
                        // We'll apply to the most recent audio source
                        audioSource = instance.source;
                    }
                }
            }

            if (!audioSource) {
                console.warn('[RadioFilters] Could not find audio source for: ' + name);
                return;
            }

            // Create filter chain if it doesn't exist
            if (!this.RadioFilters[name]) {
                this.RadioFilters[name] = {
                    lowPass: null,
                    highPass: null,
                    distortion: null,
                    source: audioSource
                };
            }

            var filters = this.RadioFilters[name];
            var filterChain = [];

            // High-pass filter (applied first in chain)
            if (enableHighPass) {
                if (!filters.highPass) {
                    filters.highPass = audioContext.createBiquadFilter();
                    filters.highPass.type = 'highpass';
                }
                filters.highPass.frequency.value = highPassCutoff;
                filterChain.push(filters.highPass);
                console.log('[RadioFilters] Applied high-pass filter: ' + highPassCutoff + ' Hz');
            } else if (filters.highPass) {
                filters.highPass.disconnect();
                filters.highPass = null;
            }

            // Low-pass filter (applied second in chain)
            if (enableLowPass) {
                if (!filters.lowPass) {
                    filters.lowPass = audioContext.createBiquadFilter();
                    filters.lowPass.type = 'lowpass';
                }
                filters.lowPass.frequency.value = lowPassCutoff;
                filterChain.push(filters.lowPass);
                console.log('[RadioFilters] Applied low-pass filter: ' + lowPassCutoff + ' Hz');
            } else if (filters.lowPass) {
                filters.lowPass.disconnect();
                filters.lowPass = null;
            }

            // Distortion/waveshaper (applied last in chain)
            if (enableDistortion && distortionAmount > 0) {
                if (!filters.distortion) {
                    filters.distortion = audioContext.createWaveShaper();

                    // Create distortion curve
                    var samples = 44100;
                    var curve = new Float32Array(samples);
                    var deg = Math.PI / 180;
                    var amount = distortionAmount;

                    for (var i = 0; i < samples; i++) {
                        var x = (i * 2 / samples) - 1;
                        curve[i] = (3 + amount) * x * 20 * deg / (Math.PI + amount * Math.abs(x));
                    }

                    filters.distortion.curve = curve;
                    filters.distortion.oversample = '4x';
                }
                filterChain.push(filters.distortion);
                console.log('[RadioFilters] Applied distortion: ' + distortionAmount);
            } else if (filters.distortion) {
                filters.distortion.disconnect();
                filters.distortion = null;
            }

            // Connect the filter chain
            if (filterChain.length > 0) {
                // Disconnect source from destination
                try {
                    audioSource.disconnect();
                } catch (e) {
                    // Source might not be connected yet
                }

                // Connect source -> filters -> destination
                var currentNode = audioSource;
                for (var i = 0; i < filterChain.length; i++) {
                    currentNode.connect(filterChain[i]);
                    currentNode = filterChain[i];
                }
                currentNode.connect(audioContext.destination);

                console.log('[RadioFilters] Filter chain connected with ' + filterChain.length + ' filters');
            } else {
                // No filters, connect directly
                try {
                    audioSource.disconnect();
                    audioSource.connect(audioContext.destination);
                } catch (e) {
                    console.warn('[RadioFilters] Could not reconnect source: ' + e);
                }
            }

        } catch (error) {
            console.error('[RadioFilters] Error applying filters: ' + error);
        }
    },

    // Remove all radio filters from an AudioSource
    RemoveRadioFiltersWebGL: function(audioSourceName) {
        var name = UTF8ToString(audioSourceName);

        if (this.RadioFilters[name]) {
            var filters = this.RadioFilters[name];

            if (filters.lowPass) {
                filters.lowPass.disconnect();
            }
            if (filters.highPass) {
                filters.highPass.disconnect();
            }
            if (filters.distortion) {
                filters.distortion.disconnect();
            }

            delete this.RadioFilters[name];
            console.log('[RadioFilters] Removed all filters from: ' + name);
        }
    }
});
