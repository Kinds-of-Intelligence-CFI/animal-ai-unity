mergeInto(LibraryManager.library, {
    NotifyExperimentComplete: function() {
        console.log("Unity: Experiment completed, notifying parent page");

        if (window.parent !== window) {
            // Error if e.g. we're in an iframe
            console.error("Unexpected deployment environment: Detected parent window");
        }

        // Also send to current window (for direct embedding)
        window.postMessage({type: 'experiment_complete'}, '*');
    }
});
