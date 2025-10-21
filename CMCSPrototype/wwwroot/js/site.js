// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Confirmation dialogs for critical actions
function confirmAction(message) {
    return confirm(message || 'Are you sure you want to proceed?');
}

// Approve claim confirmation
function confirmApprove(claimId, lecturerName) {
    return confirm(`Are you sure you want to APPROVE the claim for ${lecturerName}?`);
}

// Reject claim confirmation
function confirmReject(claimId, lecturerName) {
    return confirm(`Are you sure you want to REJECT the claim for ${lecturerName}?`);
}

// Delete confirmation
function confirmDelete(itemName) {
    return confirm(`Are you sure you want to delete ${itemName}? This action cannot be undone.`);
}

// Auto-hide alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function() {
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(function(alert) {
        setTimeout(function() {
            alert.classList.remove('show');
            setTimeout(function() {
                alert.remove();
            }, 150);
        }, 5000);
    });
});

// Form validation helper
function validateForm(formId) {
    const form = document.getElementById(formId);
    if (form) {
        return form.checkValidity();
    }
    return true;
}

// Calculate total amount dynamically
function calculateTotal() {
    const hours = parseFloat(document.getElementById('HoursWorked')?.value) || 0;
    const rate = parseFloat(document.getElementById('HourlyRate')?.value) || 0;
    const total = hours * rate;
    
    const totalElement = document.getElementById('totalAmount');
    if (totalElement) {
        totalElement.textContent = `Total: R${total.toFixed(2)}`;
    }
}

// Add event listeners if elements exist
document.addEventListener('DOMContentLoaded', function() {
    const hoursInput = document.getElementById('HoursWorked');
    const rateInput = document.getElementById('HourlyRate');
    
    if (hoursInput) {
        hoursInput.addEventListener('input', calculateTotal);
    }
    
    if (rateInput) {
        rateInput.addEventListener('input', calculateTotal);
    }
});
