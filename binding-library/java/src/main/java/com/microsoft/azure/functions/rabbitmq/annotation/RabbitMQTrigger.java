/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
 
package com.microsoft.azure.functions.rabbitmq.annotation;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

import com.microsoft.azure.functions.annotation.CustomBinding;

/**
 * <p>Place this on a parameter whose value would come from RabbitMQ and causing the method to run when a new event arrives.
 * The parameter can be one of the following</p>
 *
 * <ul>
 *     <li>Any native Java types such as int, String, byte[]</li>
 *     <li>Any POJO type</li>
 * </ul>
 *
 * <p>The following example is a Java function that uses a RabbitMQ trigger.</p>
 *
 * <pre>{@literal @}FunctionName("RabbitMQExample")
 * public void run(
 *    {@literal @}@RabbitMQTrigger(
 *               connectionStringSetting = "ConnectionString",
 *               queueName = "TestQueue"
 *           ) String input,
 *     final ExecutionContext context
 * ) {
 *     context.getLogger().info("Java RabbitMQ trigger processed a request." + input);
 * }</pre>
 *
 */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.PARAMETER)
@CustomBinding(direction = "in", name = "inputMessage", type = "rabbitMqTrigger")
public @interface RabbitMQTrigger {

    /**
     * The setting name of connection string.
     * @return The connection string setting name from app settings.
     */
    String connectionStringSetting() default "";

    /**
     * The host name to connect to.
     * @return The host name.
     */
    String hostName() default "";

    /**
     * The name of queue to connect to.
     * @return The name of queue to connect to.
     */
    String queueName() default "";

    /**
     * The username to authenticate with.
     * @return The username to authenticate with.
     */
    String userNameSetting() default "";

    /**
     * The password to authenticate with.
     * @return The password to authenticate with.
     */
    String passwordSetting() default "";

    /**
     * The VirtualHost used on the rabbitMQ Server.
     * @see <a href="https://www.rabbitmq.com/vhosts.html">Virtual Hosts</a>
     * @return The VirtualHost used on the rabbitMQ Server.
     */
    String virtualHostSetting() default "";

    /**
     * The port to attach.
     * @return The port to attach.
     */
    int port() default 0;
}
