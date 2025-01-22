document.querySelector('form').addEventListener('submit', function (event) {
    const mainImageIndex = document.getElementById('main-image-index').value;

    // Check if the main image index is empty
    if (!mainImageIndex) {
        event.preventDefault(); // Prevent form submission
        alert('Моля изберете главното изображение!'); // Display a user-friendly message
    }
});

const facilityCards = document.querySelectorAll('.facility-card');
const selectedFacilitiesInput = document.getElementById('selected-facilities');

facilityCards.forEach(card => {
    card.addEventListener('click', function () {
        const facilityId = this.dataset.id;

        // Toggle selection
        this.classList.toggle('selected');

        // Update hidden input value
        const selectedIds = Array.from(document.querySelectorAll('.facility-card.selected'))
            .map(selectedCard => selectedCard.dataset.id);
        selectedFacilitiesInput.value = selectedIds.join(',');
    });
});

let selectedImage = null; // Track the currently selected image

document.getElementById('image-input').addEventListener('change', function (event) {
    const previewContainer = document.getElementById('image-preview');
    previewContainer.innerHTML = ''; // Clear previous previews

    // Create the info message and add it to the preview container
    const infoMsg = document.createElement('p');
    infoMsg.textContent = "Моля изберете главното изображение!";
    infoMsg.classList.add('info-msg'); // Apply the blue text styling
    previewContainer.appendChild(infoMsg);

    const files = event.target.files;

    for (let i = 0; i < files.length; i++) {
        const file = files[i];

        if (file.type.startsWith('image/')) {
            const reader = new FileReader();

            reader.onload = function (e) {
                const img = document.createElement('img');
                img.src = e.target.result;
                img.dataset.index = i; // Set a unique identifier for the image
                img.classList.add('preview-image');

                img.addEventListener('click', function () {
                    // Deselect the previously selected image
                    if (selectedImage) {
                        selectedImage.classList.remove('selected');
                    }

                    // Select the current image
                    img.classList.add('selected');
                    selectedImage = img;

                    // Store the index of the selected image in a hidden input
                    document.getElementById('main-image-index').value = img.dataset.index;

                    previewContainer.removeChild(infoMsg);
                });

                previewContainer.appendChild(img);
            };

            reader.readAsDataURL(file);
        } else {
            alert('Избраните файлове трябва да са изображения.');
        }
    }
});
