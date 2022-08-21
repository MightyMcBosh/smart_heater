
#include "driver/gpio.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/i2c.h"
#include "string.h"
#include "AHT20.h"


#define LED_PIN 25


#define PIN_SCL 27
#define PIN_SDA 25 
#define I2C_MASTER_FREQ_HZ 100000



static char* TerminalMsg;  
static char msg[3];
static int NumBlinks = 0; 
static int MeasuredTemperature;

static i2c_cmd_handle_t cmd; 
static i2c_config_t config; 



void led_blink(void *pvParams) {
    gpio_num_t LedPin = LED_PIN; //convert this into an actual LED structure
    gpio_pad_select_gpio(LED_PIN);
    gpio_set_direction (LedPin,GPIO_MODE_OUTPUT);
    while (1) {
        gpio_set_level(LedPin,0);
        vTaskDelay(1000/portTICK_RATE_MS);
        gpio_set_level(LedPin,1);
        vTaskDelay(1000/portTICK_RATE_MS);
        
        

        printf(TerminalMsg);
        printf(" ");
        printf(itoa(NumBlinks++,msg,10)); 
        printf("\n"); 
        vTaskDelay(100/portTICK_RATE_MS);        
    }
}

void app_main()
{      

   TerminalMsg = "LED has blinked ";

    config.mode = I2C_MODE_MASTER;
	config.sda_io_num = PIN_SDA;
	config.sda_pullup_en = GPIO_PULLUP_DISABLE;
	config.scl_io_num = PIN_SCL;
	config.scl_pullup_en = GPIO_PULLUP_DISABLE;
	config.master.clk_speed = I2C_MASTER_FREQ_HZ;
    
    gpio_num_t LedPin = LED_PIN; //convert this into an actual LED structure
    gpio_pad_select_gpio(LED_PIN);
    gpio_set_direction (LedPin,GPIO_MODE_OUTPUT);
    
   //xTaskCreate(&led_blink,"LED_BLINK",1024,NULL,5,NULL);
    
    // do the portion of the code that handles the I2C bus and the temperature fetch functions

    printf("Building I2C Params\n");
    esp_err_t succ = AHT20_Initialize(&config, 0); 

    if(succ == ESP_OK)
    {
        printf("i2c initialization success\n");
        AHT20_readAHT20(&AHT20_cmd);
    }
    else{
        printf("i2c initialization failure\n");
    }

    
    //esp_restart(); 

}  




  
