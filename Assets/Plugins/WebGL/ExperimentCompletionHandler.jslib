mergeInto(LibraryManager.library, {
    NotifyExperimentComplete: function() {
        console.log("Unity: Experiment completed, notifying parent page");

        if (window.parent !== window) {
            // Error if e.g. we're in an iframe
            console.error("Unexpected deployment environment: Detected parent window");
        }

        window.postMessage({type: 'experiment_complete'}, '*');
    },

    NotifyUploadComplete: function() {
        console.log("Unity: CSV upload complete, notifying parent page");
        window.postMessage({type: 'upload_complete'}, '*');
    }
});
