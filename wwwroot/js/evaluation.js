/**
 * Project Evaluation System JavaScript
 */

document.addEventListener('DOMContentLoaded', function() {
    initProjectCards();
    initRatingSystem();
    initScrollAnimations();
    setupFormValidation();
});

/**
 * Initialize project card animations and effects
 */
function initProjectCards() {
    const projectCards = document.querySelectorAll('.project-card');
    
    projectCards.forEach(card => {
        // Card hover effects
        card.addEventListener('mouseenter', function() {
            this.classList.add('shadow');
            this.style.transform = 'translateY(-5px)';
        });
        
        card.addEventListener('mouseleave', function() {
            this.classList.remove('shadow');
            this.style.transform = 'translateY(0)';
        });

        // Delayed animation on load
        setTimeout(() => {
            card.classList.add('animate__animated', 'animate__fadeIn');
        }, 100 * (Array.from(projectCards).indexOf(card) % 10));
    });

    // Animate progress bars on scroll
    animateProgressBarsOnScroll();
}

/**
 * Initialize rating system functionality
 */
function initRatingSystem() {
    const ratingInputs = document.querySelectorAll('input[name="Rating"]');
    const ratingText = document.getElementById('ratingText');
    
    if (!ratingInputs.length || !ratingText) return;

    ratingInputs.forEach(input => {
        input.addEventListener('change', function() {
            const value = parseInt(this.value);
            updateRatingBadge(value, ratingText);
        });
    });

    // Add hover effects to rating buttons
    const ratingLabels = document.querySelectorAll('.btn-rating');
    ratingLabels.forEach(label => {
        label.addEventListener('mouseover', function() {
            const value = this.getAttribute('for').replace('star', '');
            const tempText = document.createElement('span');
            tempText.className = ratingText.className;
            updateRatingBadge(parseInt(value), tempText);
            
            const tempRatingText = tempText.textContent;
            document.getElementById('ratingText').setAttribute('data-original', ratingText.textContent);
            document.getElementById('ratingText').innerHTML = tempRatingText;
        });

        label.addEventListener('mouseout', function() {
            const original = document.getElementById('ratingText').getAttribute('data-original');
            if (original) {
                document.getElementById('ratingText').innerHTML = original;
            }
        });
    });
}

/**
 * Update rating badge with appropriate color and text
 */
function updateRatingBadge(value, element) {
    // Clear existing classes
    element.className = 'badge fs-6';
    
    // Set badge style and message based on rating value
    let badgeClass = 'bg-secondary';
    let message = value + ' puan';
    
    if (value >= 9) {
        badgeClass = 'bg-success';
        message += ' - Mükemmel!';
    } else if (value >= 7) {
        badgeClass = 'bg-info';
        message += ' - Çok İyi';
    } else if (value >= 5) {
        badgeClass = 'bg-primary';
        message += ' - İyi';
    } else if (value >= 3) {
        badgeClass = 'bg-warning';
        message += ' - Orta';
    } else {
        badgeClass = 'bg-danger';
        message += ' - Geliştirilmeli';
    }
    
    // Add new class and update text
    element.classList.add(badgeClass);
    element.textContent = message;
}

/**
 * Initialize scroll animations and smooth scrolling
 */
function initScrollAnimations() {
    // Smooth scroll to evaluation form
    const evaluationForm = document.getElementById('evaluationForm');
    if (evaluationForm) {
        document.querySelectorAll('a[href="#evaluationForm"]').forEach(anchor => {
            anchor.addEventListener('click', function(e) {
                e.preventDefault();
                evaluationForm.scrollIntoView({ behavior: 'smooth' });
            });
        });
    }

    // Smooth scroll for all in-page links
    document.querySelectorAll('a[href^="#"]:not([href="#"])').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            const targetId = this.getAttribute('href');
            const targetElement = document.querySelector(targetId);
            
            if (targetElement) {
                e.preventDefault();
                targetElement.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });
}

/**
 * Animate progress bars when they come into view
 */
function animateProgressBarsOnScroll() {
    const progressBars = document.querySelectorAll('.progress-bar');
    
    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const bar = entry.target;
                    const width = bar.getAttribute('aria-valuenow') + '%';
                    
                    // Animate width from 0 to target value
                    setTimeout(() => {
                        bar.style.width = "0%";
                        setTimeout(() => {
                            bar.style.width = width;
                        }, 100);
                    }, 200);
                    
                    observer.unobserve(bar);
                }
            });
        }, { threshold: 0.2 });
        
        progressBars.forEach(bar => {
            observer.observe(bar);
        });
    } else {
        // Fallback for browsers without IntersectionObserver support
        progressBars.forEach(bar => {
            const width = bar.getAttribute('aria-valuenow') + '%';
            bar.style.width = width;
        });
    }
}

/**
 * Set up form validation
 */
function setupFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');
    
    if (!forms.length) return;
    
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            
            form.classList.add('was-validated');
        }, false);
    });
}

/**
 * Show success notification
 */
function showSuccessNotification(message) {
    createNotification(message, 'success');
}

/**
 * Show error notification
 */
function showErrorNotification(message) {
    createNotification(message, 'danger');
}

/**
 * Create and display a notification
 */
function createNotification(message, type = 'info') {
    const notificationContainer = document.getElementById('notificationContainer');
    
    // Create container if it doesn't exist
    if (!notificationContainer) {
        const container = document.createElement('div');
        container.id = 'notificationContainer';
        container.style.position = 'fixed';
        container.style.top = '20px';
        container.style.right = '20px';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
    }
    
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `toast bg-${type} text-white`;
    notification.setAttribute('role', 'alert');
    notification.setAttribute('aria-live', 'assertive');
    notification.setAttribute('aria-atomic', 'true');
    
    notification.innerHTML = `
        <div class="toast-header bg-${type} text-white">
            <strong class="me-auto">Bildirim</strong>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body">
            ${message}
        </div>
    `;
    
    // Add to container
    document.getElementById('notificationContainer').appendChild(notification);
    
    // Show notification using Bootstrap Toast
    const toast = new bootstrap.Toast(notification, { 
        autohide: true,
        delay: 5000
    });
    toast.show();
    
    // Remove after hiding
    notification.addEventListener('hidden.bs.toast', function() {
        notification.remove();
    });
} 