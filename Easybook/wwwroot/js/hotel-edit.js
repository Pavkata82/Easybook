document.addEventListener('DOMContentLoaded', () => {
    const facilityCards = document.querySelectorAll('.facility-card');
    const selectedFacilitiesInput = document.getElementById('selected-facilities');
    const existingImages = document.querySelectorAll('.existing-image img');
    const deleteButtons = document.querySelectorAll('.delete-btn');
    const imagesForDeletion = document.getElementById('images-for-deletion');
    const imagePreview = document.getElementById('image-preview');
    const imageInput = document.getElementById('image-input');
    const mainImageIndexInput = document.getElementById('main-image-index');
    const mainImageWarning = document.getElementById('main-image-warning'); // The warning text element

    let currentMainImage = null; // Track the current main image

    // === Set Selected Main Image ===
    function setMainImage(selectedImg) {
        if (selectedImg.closest('.existing-image').querySelector('.delete-btn.active')) {
            alert("This image is marked for deletion and cannot be set as the main image.");
            return; // Prevent setting an image marked for deletion as the main image
        }

        document.querySelectorAll('.existing-image img, #image-preview img').forEach(img => {
            img.classList.remove('selected');
        });

        selectedImg.classList.add('selected');

        const imageId = selectedImg.getAttribute('data-image-id');
        mainImageIndexInput.value = imageId || `new-${[...imagePreview.children].indexOf(selectedImg.parentElement)}`;

        // Update the current main image tracker
        currentMainImage = selectedImg;

        // Hide the warning if the main image is set
        mainImageWarning.style.display = 'none';
    }

    // === Highlight Initial Main Image ===
    existingImages.forEach(img => {
        if (img.getAttribute('data-is-main') === 'true') {
            img.classList.add('selected');
            mainImageIndexInput.value = img.getAttribute('data-image-id');
            currentMainImage = img; // Track the initial main image
        }

        img.addEventListener('click', () => setMainImage(img));
    });

    // === Handle Delete Button Click ===
    deleteButtons.forEach(button => {
        button.addEventListener('click', () => {
            const imageId = button.getAttribute('data-image-id');
            const imageElement = button.closest('.existing-image').querySelector('img');
            const isMainImage = imageElement === currentMainImage;

            // If it's the current main image, show an alert
            if (isMainImage) {
                const confirmation = confirm("Внимание! Това е основното изображение. Ще трябва да изберете ново основно изображение преди да го изтриете.");
                if (!confirmation) {
                    return; // Don't delete if user cancels
                }

                // Optionally, deselect the main image
                imageElement.classList.remove('selected');
                mainImageIndexInput.value = ''; // Clear the main image index

                // Reset the current main image tracker
                currentMainImage = null; // User has to select a new main image

                // Show warning if main image is missing
                if (!currentMainImage) {
                    mainImageWarning.style.display = 'block'; // Show the warning text
                }
            }

            // Toggle the "active" class on the delete button
            button.classList.toggle('active');

            // Update the "imagesForDeletion" hidden input
            let currentValue = imagesForDeletion.value.split(',').filter(Boolean);

            if (button.classList.contains('active')) {
                // If the button is active, add the image ID to the deletion list
                if (!currentValue.includes(imageId)) {
                    currentValue.push(imageId);
                }
            } else {
                // If the button is inactive, remove the image ID from the deletion list
                currentValue = currentValue.filter(id => id !== imageId);
            }

            // Update the hidden input value with the modified image IDs list
            imagesForDeletion.value = currentValue.join(',');

            // If the deleted image was the main image, we need to update the image deletion list
            if (isMainImage) {
                // Add the current main image to the deletion list
                if (!currentValue.includes(imageId)) {
                    currentValue.push(imageId);
                }
                // Update the hidden input again after adding the main image
                imagesForDeletion.value = currentValue.join(',');
            }
        });
    });


    // === Handle New Image Upload ===
    imageInput.addEventListener('change', (event) => {
        const files = event.target.files;

        // If there are files, proceed
        if (files.length) {
            // Clear the existing image preview (if you want to reset the preview area)
            imagePreview.innerHTML = '';

            // Loop through each selected file and add it as a new image
            [...files].forEach(file => {
                const reader = new FileReader();

                reader.onload = (e) => {
                    const imageContainer = document.createElement('div');
                    imageContainer.classList.add('existing-image');

                    const img = document.createElement('img');
                    img.src = e.target.result;
                    img.alt = 'New Image';
                    img.addEventListener('click', () => setMainImage(img)); // To set as main image if clicked

                    imageContainer.appendChild(img);
                    imagePreview.appendChild(imageContainer);
                };

                reader.readAsDataURL(file); // Read file as data URL (for image preview)
            });
        }
    });

    // === Check for missing main image before form submission ===
    const form = document.querySelector('form'); // Assuming the images are being submitted via a form
    form.addEventListener('submit', (event) => {
        if (!currentMainImage) {
            event.preventDefault(); // Prevent form submission
            mainImageWarning.style.display = 'block'; // Show the warning text
        }
    });

});
