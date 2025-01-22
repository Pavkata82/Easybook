// wwwroot/js/datepicker.js
document.addEventListener('DOMContentLoaded', function () {
    // Get the date input elements
    const checkinDateInput = document.getElementById('checkInDate');
    const checkoutDateInput = document.getElementById('checkOutDate');

    // Event listener for when the check-in date changes
    checkinDateInput.addEventListener('input', function () {
        const checkinDate = new Date(checkinDateInput.value);

        // Set the min attribute for checkout date input to disable dates before check-in
        if (!isNaN(checkinDate.getTime())) {
            const checkinDateString = checkinDate.toISOString().split('T')[0]; // Format as YYYY-MM-DD
            checkoutDateInput.setAttribute('min', checkinDateString);

            // If the check-out date is earlier than the check-in date, reset it
            if (new Date(checkoutDateInput.value) < checkinDate) {
                checkoutDateInput.value = ''; // Clear the checkout date if it's invalid
            }
        }
    });
});
