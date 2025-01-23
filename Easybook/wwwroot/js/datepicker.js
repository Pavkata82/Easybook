document.addEventListener('DOMContentLoaded', function () {
    // Get the date input elements
    const checkinDateInput = document.getElementById('checkInDate');
    const checkoutDateInput = document.getElementById('checkOutDate');

    // Get today's date and format it as YYYY-MM-DD
    const today = new Date();
    const todayString = today.toISOString().split('T')[0];

    // Set the min attribute for the check-in date
    checkinDateInput.setAttribute('min', todayString);

    // Set the min attribute for the checkout date to one day after today
    const tomorrow = new Date(today);
    tomorrow.setDate(today.getDate() + 1);
    const tomorrowString = tomorrow.toISOString().split('T')[0];
    checkoutDateInput.setAttribute('min', tomorrowString);

    // Event listener for when the check-in date changes
    checkinDateInput.addEventListener('input', function () {
        const checkinDate = new Date(checkinDateInput.value);

        if (!isNaN(checkinDate.getTime())) {
            // Set the min attribute for checkout date to one day after the check-in date
            const checkoutMinDate = new Date(checkinDate);
            checkoutMinDate.setDate(checkinDate.getDate() + 1);
            const checkoutMinDateString = checkoutMinDate.toISOString().split('T')[0];
            checkoutDateInput.setAttribute('min', checkoutMinDateString);

            // Clear the checkout date if it's earlier than the new min
            if (new Date(checkoutDateInput.value) < checkoutMinDate) {
                checkoutDateInput.value = ''; // Clear invalid checkout date
            }
        }
    });
});
