document.querySelector('form').addEventListener('submit', function (event) {
    const mainImageIndex = document.getElementById('main-image-index').value; // Get the selected main image index

    // If no main image is selected, prevent form submission and show an alert
    if (!mainImageIndex) {
        event.preventDefault(); // Prevent form submission
        alert('Моля изберете главното изображение!'); // Display an alert to the user
    }
});

let selectedImage = null; // Track the currently selected new image for main image selection

// Handle new image upload and preview
document.getElementById('image-input').addEventListener('change', function (event) {
    const previewContainer = document.getElementById('image-preview');
    previewContainer.innerHTML = ''; // Clear any previous previews

    // Iterate through the files to preview them
    const files = event.target.files;
    for (let i = 0; i < files.length; i++) {
        const file = files[i];

        if (file.type.startsWith('image/')) {
            const reader = new FileReader();

            reader.onload = function (e) {
                const img = document.createElement('img');
                img.src = e.target.result;
                img.dataset.index = i; // Store a unique index for the image
                img.classList.add('preview-image');

                img.addEventListener('click', function () {
                    // Deselect the previously selected image
                    if (selectedImage) {
                        selectedImage.classList.remove('selected');
                    }

                    // Select the current image and mark it as the main image
                    img.classList.add('selected');
                    selectedImage = img;

                    // Set the hidden input value for the main image index
                    document.getElementById('main-image-index').value = img.dataset.index;
                });

                previewContainer.appendChild(img);
            };

            reader.readAsDataURL(file);
        } else {
            alert('Избраните файлове трябва да са изображения.');
        }
    }
});

// Handle image deletion from the gallery
document.querySelectorAll('.existing-image button').forEach(button => {
    button.addEventListener('click', function () {
        const imageId = this.dataset.imageId;

        // Send an AJAX request to delete the image (you can create an API to handle this on the backend)
        fetch(`/admin/hotels/deleteImage/${imageId}`, { method: 'DELETE' })
            .then(response => {
                if (response.ok) {
                    this.closest('.existing-image').remove();
                } else {
                    alert('Не успяхме да изтрием изображението.');
                }
            });
    });
});

// Handle existing image selection for main image
document.querySelectorAll('.existing-image img').forEach(img => {
    // If this is the main image, add the blue border immediately
    if (img.dataset.isMain === "true") {
        img.classList.add('selected');
        document.getElementById('main-image-index').value = img.dataset.imageId; // Set the hidden input to this image's ID
    }

    img.addEventListener('click', function () {
        // Deselect any previously selected image (either new or existing)
        const previouslySelected = document.querySelector('.existing-image img.selected');
        if (previouslySelected) {
            previouslySelected.classList.remove('selected');
        }

        // Deselect previously selected new images
        const newImages = document.querySelectorAll('#image-preview img.selected');
        newImages.forEach(img => img.classList.remove('selected'));

        // Select the clicked image as the main image
        img.classList.add('selected');

        // Update the hidden input value for the main image index
        document.getElementById('main-image-index').value = img.dataset.imageId;
    });
});
