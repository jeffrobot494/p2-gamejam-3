// WebGL plugin to force canvas focus
// Ensures the Unity canvas has focus so cursor state changes are properly handled

mergeInto(LibraryManager.library, {
    FocusCanvas: function() {
        if (typeof Module !== 'undefined' && Module.canvas) {
            Module.canvas.focus();
            console.log('[FocusCanvas] Canvas focused');
        }
    }
});
