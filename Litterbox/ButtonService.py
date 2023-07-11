import RPi.GPIO as GPIO
import time
import datetime;

print("Current time: ", datetime.datetime.now())

GPIO.setmode(GPIO.BCM)

GPIO.setup(2, GPIO.OUT)
GPIO.setup(3, GPIO.OUT)

try:
    GPIO.output(2, GPIO.LOW)
    GPIO.output(3, GPIO.LOW)
    print("Buttons unpressed")

    time.sleep(3)

    while True:
        GPIO.output(3, GPIO.HIGH)
        print("'Cancel' button pressed once")
        time.sleep(0.75)
        GPIO.output(3, GPIO.LOW)

        time.sleep(3)

        GPIO.output(3, GPIO.HIGH)
        print("'Cancel' button pressed twice")
        time.sleep(0.75)
        GPIO.output(3, GPIO.LOW)

        time.sleep(43200)

finally:
    GPIO.cleanup()

