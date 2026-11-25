// Custom confirmation popup (replaces the browser's "localhost says..." box)
// Usage on any submit button:  data-confirm="Message"  (optional: data-confirm-title,
// data-confirm-ok, data-confirm-variant="danger|success")
document.addEventListener('DOMContentLoaded', function () {
    const modalEl = document.getElementById('confirmModal');
    if (modalEl && window.bootstrap) {
        const modal = new bootstrap.Modal(modalEl);
        const titleEl = document.getElementById('confirmTitle');
        const messageEl = document.getElementById('confirmMessage');
        const okBtn = document.getElementById('confirmOk');
        const iconEl = document.getElementById('confirmIcon');
        let pendingForm = null;

        document.addEventListener('submit', function (e) {
            const form = e.target;
            const source = e.submitter && e.submitter.dataset.confirm ? e.submitter : null;
            if (!source || form.dataset.confirmed === 'true') return;

            e.preventDefault();
            pendingForm = form;

            const variant = source.dataset.confirmVariant || 'primary';
            titleEl.textContent = source.dataset.confirmTitle || 'Are you sure?';
            messageEl.textContent = source.dataset.confirm;
            okBtn.textContent = source.dataset.confirmOk || 'Confirm';
            okBtn.className = 'btn px-4 ' + (variant === 'danger' ? 'btn-danger' : variant === 'success' ? 'btn-success' : 'btn-primary');
            iconEl.className = 'confirm-icon mb-3 ' + variant;
            iconEl.innerHTML = variant === 'danger'
                ? '<i class="bi bi-trash3"></i>'
                : variant === 'success' ? '<i class="bi bi-check2-circle"></i>' : '<i class="bi bi-question-lg"></i>';

            modal.show();
        });

        okBtn.addEventListener('click', function () {
            if (!pendingForm) return;
            pendingForm.dataset.confirmed = 'true';
            okBtn.disabled = true;
            pendingForm.submit();
        });

        modalEl.addEventListener('hidden.bs.modal', function () {
            okBtn.disabled = false;
            pendingForm = null;
        });
    }

    // Success messages fade away after 5 seconds
    document.querySelectorAll('.alert-success.alert-dismissible').forEach(function (el) {
        setTimeout(function () {
            if (window.bootstrap) bootstrap.Alert.getOrCreateInstance(el).close();
        }, 5000);
    });
});