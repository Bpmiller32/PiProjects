from machine import Pin # type: ignore
from time import ticks_ms, ticks_diff, sleep
# import urequests

# Configuration, global variables
SENSOR_PIN = 28
WEBHOOK_URL = "https://discord.com/api/webhooks/1294125716098781236/HPMSB1cSwAiVjIGNw2Ty3snbFGtPzRoPBuzMhrjWj0_jQNzjh8eAZ3xbscrBgrENXc0L"

GREEN_LED_PIN = 16
YELLOW_LED_PIN = 17
ORANGE_LED_PIN = 18
RED_LED_PIN = 19  

THRESHOLD_GREEN = 40
THRESHOLD_YELLOW = 90
THRESHOLD_ORANGE = 190
THRESHOLD_RED = 390

# Number of detected pulses in the current period, time when the last pulse was detected, total elapsed time (in seconds) between pulses
pulse_count = 0  
last_pulse_time = ticks_ms()  
elapsed_time = 0  


# Interrupt handler for counting pulses, called automatically whenever a rising edge is detected on the sensor pin. Updates the pulse count and calculates the time difference since the last pulse.
def handle_pulse(pin):
    # Grab globals
    global pulse_count, last_pulse_time, elapsed_time

    # Get the current time in milliseconds
    current_time = ticks_ms() 
    # Increment the pulse count
    pulse_count += 1  
    
    # Calculate the time difference since the last pulse and convert to seconds
    elapsed_time += ticks_diff(current_time, last_pulse_time) / 1000.0
    # Update the last pulse time
    last_pulse_time = current_time  

# Calculate the frequency of the pulses based on the pulse count and elapsed time.
def calculate_frequency():
    if elapsed_time > 0: 
        # Frequency = Pulses / Time
        return pulse_count / elapsed_time  
    
    # No pulses detected, return 0 Hz
    return 0.0 

# Send a message to the Discord webhook
def send_discord_alert(message):
    try:
        # Payload for the Discord webhook
        payload = {
            "content": message
        }
        # Send the POST request to the webhook URL
        response = urequests.post(WEBHOOK_URL, json=payload)
        # Close the response to free up resources
        response.close()
    except Exception as e:
        print(f"Error sending Discord alert: {e}")

def main():
    # Grab globals
    global pulse_count, elapsed_time

    # Set up the sensor GPIO pin as an input with a pull-up resistor, attach an interrupt to the pin to detect rising edges
    sensor_pin = Pin(SENSOR_PIN, Pin.IN, Pin.PULL_UP)
    sensor_pin.irq(trigger=Pin.IRQ_RISING, handler=handle_pulse)

    # Set up the LEDs as outputs, turn off LEDs initially
    green_led = Pin(GREEN_LED_PIN, Pin.OUT)
    yellow_led = Pin(YELLOW_LED_PIN, Pin.OUT)
    orange_led = Pin(ORANGE_LED_PIN, Pin.OUT)
    red_led = Pin(RED_LED_PIN, Pin.OUT)
    
    green_led.value(0)
    yellow_led.value(0)
    orange_led.value(0)
    red_led.value(0)

    print("Press Ctrl+C to stop the program")

    try:
        while True:
            # Wait for 1 second before calculating the frequency
            sleep(0.25) 
            # Get the current frequency
            frequency = calculate_frequency()  
            
            if frequency > 0:
                print(f"Pulse frequency: {frequency:.2f} Hz")
            
                # Light up LEDs based on the frequency
                if frequency > THRESHOLD_RED:
                    red_led.value(1)
                    orange_led.value(1)
                    yellow_led.value(1)
                    green_led.value(1)
                elif frequency > THRESHOLD_ORANGE:
                    red_led.value(0)
                    orange_led.value(1)
                    yellow_led.value(1)
                    green_led.value(1)
                elif frequency > THRESHOLD_YELLOW:
                    red_led.value(0)
                    orange_led.value(0)
                    yellow_led.value(1)
                    green_led.value(1)
                elif frequency > THRESHOLD_GREEN:
                    red_led.value(0)
                    orange_led.value(0)
                    yellow_led.value(0)
                    green_led.value(1)
                else:
                    red_led.value(0)
                    orange_led.value(0)
                    yellow_led.value(0)
                    green_led.value(0)
            else:
                print("No pulses detected")  

            # Reset the pulse count and elapsed time for the next measurement period
            pulse_count = 0
            elapsed_time = 0
    except KeyboardInterrupt:
        # Gracefully handle Ctrl+C (KeyboardInterrupt) to terminate the program
        print("\nProgram terminated")

# Entry point of the program
if __name__ == "__main__":
    main()