// Photo upload component: drag and drop, preview, and quick checks before the form is sent.
// The server still validates everything again (see Helpers/PhotoHelper.cs).
(function () {
    'use strict';

    var allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];

    function setup(root) {
        if (root.dataset.ready) return;
        root.dataset.ready = 'true';

        var input = root.querySelector('.photo-upload-input');
        var image = root.querySelector('.photo-upload-preview img');
        var nameLabel = root.querySelector('.photo-upload-name');
        var removeButton = root.querySelector('.photo-upload-remove');
        var errorBox = root.querySelector('.photo-upload-error');
        var maxBytes = parseInt(root.dataset.maxBytes, 10);
        var objectUrl = null;

        function showError(message) {
            errorBox.textContent = message;
            errorBox.hidden = !message;
        }

        function clear() {
            input.value = '';
            if (objectUrl) URL.revokeObjectURL(objectUrl);
            objectUrl = null;
            image.removeAttribute('src');
            nameLabel.textContent = '';
            root.classList.remove('has-photo');
        }

        function show(file) {
            showError('');

            if (allowedTypes.indexOf(file.type) === -1) {
                clear();
                showError('Only ' + root.dataset.allowedText + ' photos are allowed.');
                return;
            }
            if (file.size > maxBytes) {
                clear();
                showError('The photo is too large. The maximum size is ' + root.dataset.maxText + '.');
                return;
            }

            if (objectUrl) URL.revokeObjectURL(objectUrl);
            objectUrl = URL.createObjectURL(file);
            image.src = objectUrl;
            nameLabel.textContent = file.name;
            root.classList.add('has-photo');
        }

        input.addEventListener('change', function () {
            if (input.files.length) show(input.files[0]);
            else clear();
        });

        removeButton.addEventListener('click', function () {
            showError('');
            clear();
        });

        // Drag and drop onto the drop area
        ['dragenter', 'dragover'].forEach(function (name) {
            root.addEventListener(name, function (e) {
                e.preventDefault();
                root.classList.add('is-dragging');
            });
        });
        ['dragleave', 'drop'].forEach(function (name) {
            root.addEventListener(name, function (e) {
                e.preventDefault();
                root.classList.remove('is-dragging');
            });
        });
        root.addEventListener('drop', function (e) {
            if (!e.dataTransfer.files.length) return;
            input.files = e.dataTransfer.files;
            show(input.files[0]);
        });
    }

    document.querySelectorAll('[data-photo-upload]').forEach(setup);
})();
