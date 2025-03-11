document.addEventListener('DOMContentLoaded', () => {
    const existingImages = document.querySelectorAll('.existing-image img');
    const imagePreview = document.getElementById('image-preview');
    const imageInput = document.getElementById('image-input');
    const mainImageIndexInput = document.getElementById('main-image-index');

    // Function to set the selected main image
    function setMainImage(selectedImg) {
        // Remove the "selected" class from all images (existing + new)
        document.querySelectorAll('.existing-image img, #image-preview img').forEach(img => {
            img.classList.remove('selected');
        });

        // Add "selected" class to the clicked image
        selectedImg.classList.add('selected');

        // Update the hidden input value (for form submission)
        const imageId = selectedImg.getAttribute('data-image-id');
        if (imageId) {
            mainImageIndexInput.value = imageId;
        } else {
            const index = [...imagePreview.children].indexOf(selectedImg.parentElement);
            mainImageIndexInput.value = `new-${index}`;
        }
    }

    // Highlight the initial main image among existing ones
    existingImages.forEach(img => {
        if (img.getAttribute('data-is-main') === 'true') {
            img.classList.add('selected');
            mainImageIndexInput.value = img.getAttribute('data-image-id');
        }

        img.addEventListener('click', () => setMainImage(img));
    });

    // Handle new images added by user
    imageInput.addEventListener('change', (event) => {
        const files = event.target.files;
        if (files.length) {
            [...files].forEach((file, index) => {
                const reader = new FileReader();

                reader.onload = (e) => {
                    const imageContainer = document.createElement('div');
                    imageContainer.classList.add('existing-image');

                    const img = document.createElement('img');
                    img.src = e.target.result;
                    img.alt = 'New Image';
                    img.classList.add('existing-image-thumbnail');

                    img.addEventListener('click', () => setMainImage(img));

                    imageContainer.appendChild(img);
                    imagePreview.appendChild(imageContainer);
                };

                reader.readAsDataURL(file);
            });
        }
    });
});