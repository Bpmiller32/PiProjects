import RPi.GPIO as GPIO
import time

GPIO.setmode(GPIO.BCM)

GPIO.setup(2, GPIO.OUT)
GPIO.setup(3, GPIO.OUT)

try:
    GPIO.output(2, GPIO.LOW)
    GPIO.output(3, GPIO.LOW)
    print("Buttons unpressed")

    while True:
        user_input = input("1 to press 'Spin', 2 to press 'Cancel', 3 to exit")

        if user_input == '1':
            GPIO.output(2, GPIO.HIGH)
            print("'Spin' button pressed")
            time.sleep(0.75)
            GPIO.output(2, GPIO.LOW)
        if user_input == '2':
            GPIO.output(3, GPIO.HIGH)
            print("'Cancel' button pressed")
            time.sleep(0.75)
            GPIO.output(3, GPIO.LOW)
        if user_input == '3':
            break

finally:
    GPIO.cleanup()
