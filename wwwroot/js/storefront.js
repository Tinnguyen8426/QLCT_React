(function () {
    if (typeof window === "undefined" || !window.document) {
        return;
    }

    const toastContainerId = "cart-toast-container";

    const updateCartBadge = (quantity) => {
        const badge = document.getElementById("cart-count-badge");
        if (badge) {
            badge.textContent = quantity;
        }

        document.querySelectorAll("[data-cart-count-target], .cart-count-display").forEach(el => {
            el.textContent = quantity;
        });
    };

    const showToast = (message, success = true) => {
        let container = document.getElementById(toastContainerId);
        if (!container) {
            container = document.createElement("div");
            container.id = toastContainerId;
            container.style.position = "fixed";
            container.style.top = "1rem";
            container.style.right = "1rem";
            container.style.zIndex = "1080";
            document.body.appendChild(container);
        }

        const alert = document.createElement("div");
        alert.className = `alert alert-${success ? "success" : "danger"} shadow-sm`;
        alert.textContent = message;
        container.appendChild(alert);

        setTimeout(() => {
            alert.classList.add("fade");
            setTimeout(() => alert.remove(), 300);
        }, 3000);
    };

    const loadGooglePlaces = (apiKey, onLoaded) => {
        if (window.google && window.google.maps && window.google.maps.places) {
            if (typeof onLoaded === "function") onLoaded();
            return;
        }
        if (!apiKey) return;

        const existing = document.getElementById("google-places-script");
        if (existing) {
            existing.addEventListener("load", () => {
                if (typeof onLoaded === "function") onLoaded();
            });
            return;
        }

        const script = document.createElement("script");
        script.id = "google-places-script";
        script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&libraries=places`;
        script.async = true;
        script.defer = true;
        script.onload = () => {
            if (typeof onLoaded === "function") onLoaded();
        };
        document.head.appendChild(script);
    };

    const initAddressAutocomplete = () => {
        const addressInput = document.querySelector(".address-autocomplete");
        if (!addressInput) return;

        const apiKey = document.querySelector('meta[name="google-places-key"]')?.content;
        const setup = () => {
            if (!(window.google && window.google.maps && window.google.maps.places)) return;
            const autocomplete = new google.maps.places.Autocomplete(addressInput, {
                types: ["address"],
                componentRestrictions: { country: "vn" }
            });
            autocomplete.addListener("place_changed", () => {
                const place = autocomplete.getPlace();
                if (place && place.formatted_address) {
                    addressInput.value = place.formatted_address;
                }
            });
        };

        if (window.google && window.google.maps && window.google.maps.places) {
            setup();
        } else {
            loadGooglePlaces(apiKey, setup);
        }
    };

    const registerAjaxForm = (form, options = {}) => {
        if (!form || form.dataset.ajaxBound === "true") {
            return;
        }

        const handler = async (event) => {
            if (!window.fetch) {
                return;
            }

            event.preventDefault();

            const formData = new FormData(form);
            const action = form.getAttribute("action") || window.location.pathname;
            const method = (form.getAttribute("method") || "POST").toUpperCase();

            try {
                const response = await fetch(action, {
                    method,
                    body: formData,
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                });

                if (!response.ok) {
                    throw new Error("Request failed");
                }

                const data = await response.json();
                if (typeof data.cartQuantity !== "undefined") {
                    updateCartBadge(data.cartQuantity);
                }
                if (data.message) {
                    showToast(data.message, data.success !== false);
                }

                if (data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                    return;
                }

                if (typeof options.onSuccess === "function") {
                    options.onSuccess(data, form);
                }
            }
            catch (error) {
                form.removeEventListener("submit", handler);
                form.submit();
            }
        };

        form.addEventListener("submit", handler);
        form.dataset.ajaxBound = "true";
    };

    const init = () => {
        document.querySelectorAll(".add-to-cart-form").forEach(form => registerAjaxForm(form));
        document.querySelectorAll(".cart-update-form").forEach(form => registerAjaxForm(form, {
            onSuccess: () => window.location.reload()
        }));
        document.querySelectorAll(".cart-remove-form").forEach(form => registerAjaxForm(form, {
            onSuccess: () => window.location.reload()
        }));
        document.querySelectorAll(".cart-checkout-form").forEach(form => registerAjaxForm(form, {
            onSuccess: () => window.location.reload()
        }));
        initAddressAutocomplete();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
