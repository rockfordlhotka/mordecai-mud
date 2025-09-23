// Admin panel JavaScript utilities

// Modal management functions
window.showModal = (modalId) => {
    const modal = new bootstrap.Modal(document.getElementById(modalId));
    modal.show();
};

window.hideModal = (modalId) => {
    const modalElement = document.getElementById(modalId);
    const modal = bootstrap.Modal.getInstance(modalElement);
    if (modal) {
        modal.hide();
    }
};

// Confirmation dialog
window.confirm = (message) => {
    return confirm(message);
};

// Initialize tooltips when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Bootstrap tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});