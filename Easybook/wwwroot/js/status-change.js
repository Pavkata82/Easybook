// Function to handle the status change confirmation for button click
function confirmStatusChangeForButton(buttonElement) {
    const selectedStatus = buttonElement.textContent.trim();  // Get the status text

    // Show the confirmation dialog
    const confirmed = confirm(`Сигурни ли сте, че искате да промените статуса на "${selectedStatus}"?`);

    // If confirmed, submit the form
    if (confirmed) {
        // Find the form associated with this button and submit it
        const form = buttonElement.closest("form");
        form.submit();
    }
    else {
        // If cancelled, do nothing (status won't change)
        console.log("Status change canceled.");
    }
}

// Function to handle the status change confirmation for select dropdown
function confirmStatusChangeForSelect(selectElement) {
    const selectedValue = selectElement.value;
    const selectedText = selectElement.options[selectElement.selectedIndex].text;

    // Show a confirmation dialog in Bulgarian
    const confirmed = confirm(`Сигурни ли сте, че искате да промените статуса на "${selectedText}"?`);

    // If the user clicks 'Cancel', reset the select value back to the original one
    if (!confirmed) {
        selectElement.value = selectElement.dataset.previousValue;
    }
    else {
        // If confirmed, update the previous value of the select dropdown
        selectElement.dataset.previousValue = selectedValue;
        // Submit the form
        selectElement.form.submit();
    }
}
