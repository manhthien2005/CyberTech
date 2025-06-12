// Countdown timer for flash sales

document.addEventListener("DOMContentLoaded", () => {
    // Get countdown elements
    const hoursElement = document.getElementById("hours")
    const minutesElement = document.getElementById("minutes")
    const secondsElement = document.getElementById("seconds")

    if (hoursElement && minutesElement && secondsElement) {
        // Set the countdown end time (3 hours from now)
        const now = new Date()
        const endTime = new Date(now.getTime() + 3 * 60 * 60 * 1000)

        // Update countdown every second
        const countdownInterval = setInterval(updateCountdown, 1000)

        function updateCountdown() {
            // Get current time
            const currentTime = new Date()

            // Calculate remaining time in milliseconds
            const remainingTime = endTime - currentTime

            // If countdown is finished
            if (remainingTime <= 0) {
                clearInterval(countdownInterval)
                hoursElement.textContent = "00"
                minutesElement.textContent = "00"
                secondsElement.textContent = "00"
                return
            }

            // Calculate hours, minutes, and seconds
            const hours = Math.floor(remainingTime / (1000 * 60 * 60))
            const minutes = Math.floor((remainingTime % (1000 * 60 * 60)) / (1000 * 60))
            const seconds = Math.floor((remainingTime % (1000 * 60)) / 1000)

            // Update the countdown display
            hoursElement.textContent = hours < 10 ? "0" + hours : hours
            minutesElement.textContent = minutes < 10 ? "0" + minutes : minutes
            secondsElement.textContent = seconds < 10 ? "0" + seconds : seconds
        }

        // Initial update
        updateCountdown()
    }
})
