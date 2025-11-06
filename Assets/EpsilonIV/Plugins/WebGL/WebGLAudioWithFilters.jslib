mergeInto(LibraryManager.library, {

    // REPLACEMENT for SDK's PlayWebGLAudio - uses Web Audio API with filters
    PlayWebGLAudio: function(identifierPtr, base64AudioPtr) {
        var identifier = UTF8ToString(identifierPtr);
        var base64Audio = UTF8ToString(base64AudioPtr);

        console.log('[WebGLAudio] Playing audio for ' + identifier);

        // Initialize volume storage if not already done
        if (!window._webGLAudioVolumes) {
            window._webGLAudioVolumes = {};
        }

        // Initialize storage on window if not already done
        if (!window._webGLAudioInstances) {
            window._webGLAudioInstances = {};
        }
        if (!window._pendingFilterSettings) {
            window._pendingFilterSettings = {};
        }

        try {
            // Stop any existing audio for this identifier
            if (window._webGLAudioInstances[identifier]) {
                var oldInstance = window._webGLAudioInstances[identifier];
                if (oldInstance && oldInstance.audio) {
                    oldInstance.audio.pause();
                    oldInstance.audio.currentTime = 0;
                }
            }

            // Initialize Web Audio context
            if (!window._webGLAudioContext) {
                try {
                    var AudioContext = window.AudioContext || window.webkitAudioContext;
                    window._webGLAudioContext = new AudioContext();
                    console.log('[WebGLAudio] Audio context created');
                } catch (e) {
                    console.error('[WebGLAudio] Failed to create audio context:', e);
                }
            }
            var audioContext = window._webGLAudioContext;
            if (!audioContext) {
                console.error('[WebGLAudio] No audio context available');
                return;
            }

            // Decode base64 to binary
            var binaryString = atob(base64Audio);
            var bytes = new Uint8Array(binaryString.length);
            for (var i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }

            // Create blob and object URL
            var blob = new Blob([bytes], { type: 'audio/mpeg' });
            var audioUrl = URL.createObjectURL(blob);

            // Create HTML5 audio element
            var audio = new Audio(audioUrl);
            audio.crossOrigin = 'anonymous';

            // Create Web Audio source from the audio element
            var source = audioContext.createMediaElementSource(audio);

            // Check if Unity sent filter settings before audio was created
            var filters;
            if (window._pendingFilterSettings[identifier]) {
                console.log('[WebGLAudio] Using pending filter settings from Unity for ' + identifier);
                filters = window._pendingFilterSettings[identifier];
                delete window._pendingFilterSettings[identifier];
            } else {
                // Use default radio filter settings
                console.log('[WebGLAudio] Using default filter settings for ' + identifier);
                filters = {
                    enableLowPass: true,
                    lowPassCutoff: 3000,
                    enableHighPass: true,
                    highPassCutoff: 300,
                    enableDistortion: false,
                    distortionLevel: 1
                };
            }

            // Create filter nodes
            var highPass = null;
            var lowPass = null;
            var distortion = null;
            var gainNode = audioContext.createGain();
            var nodeChain = [];

            // High-pass filter (remove low rumble)
            if (filters.enableHighPass) {
                highPass = audioContext.createBiquadFilter();
                highPass.type = 'highpass';
                highPass.frequency.value = filters.highPassCutoff;
                nodeChain.push(highPass);
                console.log('[WebGLAudio] Applied high-pass filter: ' + filters.highPassCutoff + ' Hz');
            }

            // Low-pass filter (radio frequency range)
            if (filters.enableLowPass) {
                lowPass = audioContext.createBiquadFilter();
                lowPass.type = 'lowpass';
                lowPass.frequency.value = filters.lowPassCutoff;
                nodeChain.push(lowPass);
                console.log('[WebGLAudio] Applied low-pass filter: ' + filters.lowPassCutoff + ' Hz');
            }

            // Distortion/waveshaper (analog radio character)
            if (filters.enableDistortion && filters.distortionLevel > 0) {
                distortion = audioContext.createWaveShaper();

                // Create distortion curve with aggressive soft clipping
                var samples = 44100;
                var curve = new Float32Array(samples);
                var amount = filters.distortionLevel;
                var gain = 1 + (amount * 50); // Scale: 0=1x, 1=51x gain

                for (var i = 0; i < samples; i++) {
                    var x = (i * 2 / samples) - 1;
                    // Soft clipping using hyperbolic tangent approximation
                    var amplified = x * gain;
                    curve[i] = amplified / (1 + Math.abs(amplified));
                }

                distortion.curve = curve;
                distortion.oversample = '4x';
                nodeChain.push(distortion);
                console.log('[WebGLAudio] Applied distortion: ' + filters.distortionLevel);
            }

            // Always add gain node at the end
            nodeChain.push(gainNode);

            // Set gain node volume (check if volume was set, otherwise default to 1.0)
            var volume = window._webGLAudioVolumes[identifier] !== undefined ? window._webGLAudioVolumes[identifier] : 1.0;
            gainNode.gain.value = volume;
            console.log('[WebGLAudio] Set gain node volume to ' + volume + ' for ' + identifier);

            // Connect the filter chain: source -> filters -> gainNode -> destination
            try {
                var currentNode = source;
                for (var i = 0; i < nodeChain.length; i++) {
                    currentNode.connect(nodeChain[i]);
                    currentNode = nodeChain[i];
                }
                currentNode.connect(audioContext.destination);

                console.log('[WebGLAudio] Filter chain connected (' + (nodeChain.length - 1) + ' filters)');
            } catch (error) {
                console.error('[WebGLAudio] Error connecting filter chain:', error);
            }

            // Store instance
            window._webGLAudioInstances[identifier] = {
                audio: audio,
                source: source,
                audioUrl: audioUrl,
                highPass: highPass,
                lowPass: lowPass,
                distortion: distortion,
                gainNode: gainNode,
                filters: filters
            };

            // Play audio
            audio.play().then(function() {
                console.log('[WebGLAudio] Audio playing for ' + identifier);
            }).catch(function(error) {
                console.error('[WebGLAudio] Failed to play audio for ' + identifier + ':', error);
            });

            // Cleanup when audio ends
            audio.onended = function() {
                console.log('[WebGLAudio] Audio ended for ' + identifier);
                // Cleanup inline
                var inst = window._webGLAudioInstances[identifier];
                if (inst) {
                    try {
                        if (inst.source) inst.source.disconnect();
                        if (inst.highPass) inst.highPass.disconnect();
                        if (inst.lowPass) inst.lowPass.disconnect();
                        if (inst.distortion) inst.distortion.disconnect();
                        if (inst.gainNode) inst.gainNode.disconnect();
                    } catch (e) {}
                    delete window._webGLAudioInstances[identifier];
                }
            };

            // Cleanup blob URL after audio loads
            audio.onloadeddata = function() {
                URL.revokeObjectURL(audioUrl);
            };

        } catch (error) {
            console.error('[WebGLAudio] Error playing audio for ' + identifier + ':', error);
        }
    },

    // Update filter parameters (called from Unity's RadioAudioPlayer.cs)
    ApplyRadioFiltersWebGL: function(identifierPtr, enableLowPass, lowPassCutoff, enableHighPass, highPassCutoff, enableDistortion, distortionLevel) {
        var identifier = UTF8ToString(identifierPtr);

        // Initialize storage on window if not already done
        if (!window._webGLAudioInstances) {
            window._webGLAudioInstances = {};
        }
        if (!window._pendingFilterSettings) {
            window._pendingFilterSettings = {};
        }

        var instance = window._webGLAudioInstances[identifier];
        if (!instance) {
            // Audio hasn't been created yet - store settings for when PlayWebGLAudio is called
            console.log('[WebGLAudio] Storing pending filter settings for ' + identifier);
            window._pendingFilterSettings[identifier] = {
                enableLowPass: enableLowPass,
                lowPassCutoff: lowPassCutoff,
                enableHighPass: enableHighPass,
                highPassCutoff: highPassCutoff,
                enableDistortion: enableDistortion,
                distortionLevel: distortionLevel
            };
            return;
        }

        console.log('[WebGLAudio] Updating filters for existing audio: ' + identifier);

        // Update filter settings on existing instance
        instance.filters.enableLowPass = enableLowPass;
        instance.filters.lowPassCutoff = lowPassCutoff;
        instance.filters.enableHighPass = enableHighPass;
        instance.filters.highPassCutoff = highPassCutoff;
        instance.filters.enableDistortion = enableDistortion;
        instance.filters.distortionLevel = distortionLevel;

        // Reapply filters (inline)
        var audioContext = window._webGLAudioContext;
        if (!audioContext) return;

        var nodeChain = [];

        // High-pass filter
        if (instance.filters.enableHighPass) {
            if (!instance.highPass) {
                instance.highPass = audioContext.createBiquadFilter();
                instance.highPass.type = 'highpass';
            }
            instance.highPass.frequency.value = instance.filters.highPassCutoff;
            nodeChain.push(instance.highPass);
        }

        // Low-pass filter
        if (instance.filters.enableLowPass) {
            if (!instance.lowPass) {
                instance.lowPass = audioContext.createBiquadFilter();
                instance.lowPass.type = 'lowpass';
            }
            instance.lowPass.frequency.value = instance.filters.lowPassCutoff;
            nodeChain.push(instance.lowPass);
        }

        // Distortion
        if (instance.filters.enableDistortion && instance.filters.distortionLevel > 0) {
            if (!instance.distortion) {
                instance.distortion = audioContext.createWaveShaper();
                var samples = 44100;
                var curve = new Float32Array(samples);
                var amount = instance.filters.distortionLevel;
                var gain = 1 + (amount * 50); // Scale: 0=1x, 1=51x gain
                for (var i = 0; i < samples; i++) {
                    var x = (i * 2 / samples) - 1;
                    // Soft clipping using hyperbolic tangent approximation
                    var amplified = x * gain;
                    curve[i] = amplified / (1 + Math.abs(amplified));
                }
                instance.distortion.curve = curve;
                instance.distortion.oversample = '4x';
            }
            nodeChain.push(instance.distortion);
        }

        // Always add gain node
        nodeChain.push(instance.gainNode);

        // Reconnect chain
        try {
            instance.source.disconnect();
            var currentNode = instance.source;
            for (var i = 0; i < nodeChain.length; i++) {
                currentNode.connect(nodeChain[i]);
                currentNode = nodeChain[i];
            }
            currentNode.connect(audioContext.destination);
        } catch (error) {
            console.error('[WebGLAudio] Error reconnecting filter chain:', error);
        }
    },

    // Remove all filters (called from Unity)
    RemoveRadioFiltersWebGL: function(identifierPtr) {
        var identifier = UTF8ToString(identifierPtr);
        var instance = window._webGLAudioInstances[identifier];
        if (instance && instance.audio) {
            instance.audio.pause();
            instance.audio.currentTime = 0;
            // Cleanup inline
            try {
                if (instance.source) instance.source.disconnect();
                if (instance.highPass) instance.highPass.disconnect();
                if (instance.lowPass) instance.lowPass.disconnect();
                if (instance.distortion) instance.distortion.disconnect();
                if (instance.gainNode) instance.gainNode.disconnect();
            } catch (e) {}
            delete window._webGLAudioInstances[identifier];
        }
        console.log('[WebGLAudio] Removed all filters for ' + identifier);
    },

    // Set volume for WebGL audio (called from Unity)
    SetWebGLAudioVolume: function(identifierPtr, volume) {
        var identifier = UTF8ToString(identifierPtr);

        // Initialize storage if needed
        if (!window._webGLAudioVolumes) {
            window._webGLAudioVolumes = {};
        }

        // Store the volume for this identifier
        window._webGLAudioVolumes[identifier] = volume;
        console.log('[WebGLAudio] Stored volume ' + volume + ' for ' + identifier);

        // If audio instance already exists, update its gain node immediately
        if (window._webGLAudioInstances && window._webGLAudioInstances[identifier]) {
            var instance = window._webGLAudioInstances[identifier];
            if (instance.gainNode) {
                instance.gainNode.gain.value = volume;
                console.log('[WebGLAudio] Updated gain node volume to ' + volume + ' for active audio ' + identifier);
            }
        }
    }

});
