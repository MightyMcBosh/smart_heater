
#define LOG_LOCAL_LEVEL ESP_LOG_VERBOSE

#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/gpio.h"
#include "driver/i2c.h"

#include "AHT20.h"


esp_err_t AHT20_Initialize(i2c_config_t *config, int port)
{
    AHT20_i2c_ret = ESP_OK;
    if (port < 0 || port > 1)
    {
        AHT20_i2c_ret = ESP_FAIL;
    }
    else
    {
        // build out the i2c object
        AHT20_port = port;
        AHT20_i2c_ret = i2c_param_config(AHT20_port, config);
        AHT20_i2c_ret = i2c_driver_install(AHT20_port, config->mode, 16, 16, 0);
        AHT20_cmd = i2c_cmd_link_create();
        //check calibration
        
        printf("Initialization OK");
    }

    return AHT20_i2c_ret;
}

esp_err_t AHT20_Erase()
{
    i2c_master_stop(AHT20_cmd);
    i2c_driver_delete(AHT20_port);
    return ESP_OK;
}


/// this reads the values into the internal static values
/// it needs to be called from within a task or it won't work right.
esp_err_t AHT20_readAHT20()
{
    AHT20_i2c_ret = ESP_FAIL; 
    // write
    i2c_master_start(AHT20_cmd);
    i2c_master_write_byte(AHT20_cmd, AHT_ADDR | I2C_MASTER_WRITE, true);
    i2c_master_write_byte(AHT20_cmd, AHTX0_CMD_TRIGGER, true);
    i2c_master_write_byte(AHT20_cmd, AHTX0_CMD_0, true);
    i2c_master_write_byte(AHT20_cmd, AHTX0_CMD_1, true);
    i2c_master_stop(AHT20_cmd);
    AHT20_i2c_ret = i2c_master_cmd_begin(AHT20_port,AHT20_cmd,100/portTICK_RATE_MS); 
    i2c_cmd_link_delete(AHT20_cmd);
    //Wait
    if(AHT20_i2c_ret == ESP_OK)
    {
        printf("CMDs Successful");
        // read data 

        AHT20_cmd = i2c_cmd_link_create(); 
        i2c_master_start(AHT20_cmd);
        enum i2c_ack_type_t t = data; 
        i2c_master_read_byte(AHT20_cmd, &AHT20_data[0], t);
        i2c_master_read_byte(AHT20_cmd, &AHT20_data[1], t);
        i2c_master_read_byte(AHT20_cmd, &AHT20_data[2], t);
        i2c_master_read_byte(AHT20_cmd, &AHT20_data[3], t);
        i2c_master_read_byte(AHT20_cmd, &AHT20_data[4], t);
        i2c_master_read_byte(AHT20_cmd, &AHT20_data[5], t);
        i2c_master_stop(AHT20_cmd); 
        AHT20_i2c_ret = i2c_master_cmd_begin(AHT20_port,AHT20_cmd,100/portTICK_RATE_MS); 
        if(AHT20_i2c_ret != ESP_OK)
        {
            printf("Read Unsuccessful"); 
        }
        else
        {
            printf("Read Successful!\n");
            printf("Data: \n");
            for(int i = 0; i < 6; i++)
            {
                printf("%i\n",AHT20_data[0]);
            }
        }
    }
    else
    {
        printf("CMD unsuccessful");
    }

    return ESP_OK; 
    
}

float AHT20_getHumidity()
{
    return AHT20_humidity;
}
float AHT20_getTemperature()
{
    return AHT20_temperature;
}
