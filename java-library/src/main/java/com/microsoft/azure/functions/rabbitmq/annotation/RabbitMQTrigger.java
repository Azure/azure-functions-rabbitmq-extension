/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.rabbitmq.annotation;

import com.microsoft.azure.functions.annotation.CustomBinding;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * <p>
 * Java annotation used to bind a parameter to RabbitMQ trigger message. The
 * type of parameter can be a native Java type such as <code>int</code>,
 * <code>String</code> or <code>byte[]</code> or a POJO.
 * </p>
 *
 * <p>
 * Example function that uses a RabbitMQ trigger binding:
 * </p>
 *
 * <pre>
 * &#64;FunctionName("RabbitMQExample")
 * public void run(
 *         &#64;RabbitMQTrigger(connectionStringSetting = "ConnectionString", queueName = "input-queue") String message,
 *         final ExecutionContext context) {
 *     context.getLogger().info("Java RabbitMQ trigger function processed a message: " + message);
 * }
 * </pre>
 */

@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.PARAMETER)
@CustomBinding(direction = "in", name = "inputMessage", type = "rabbitMqTrigger")
public @interface RabbitMQTrigger {

    /**
     * Setting name for RabbitMQ connection URI.
     * @return Setting name for RabbitMQ connection URI.
     */
    String connectionStringSetting() default "";

    /**
     * <p>Defines how Functions runtime should treat the parameter value. Possible values are:</p>
     * <ul>
     *     <li>"": get the value as a string, and try to deserialize to actual parameter type like POJO</li>
     *     <li>string: always get the value as a string</li>
     *     <li>binary: get the value as a binary data, and try to deserialize to actual parameter type byte[]</li>
     * </ul>
     * 
     * @return The dataType which will be used by the Functions runtime.
     */
    String dataType() default "";

    /**
     * Whether certificate validation should be disabled. Not recommended for production. Does not apply when SSL is
     * disabled.
     *
     * @return Whether certificate validation should be disabled.
     */
    boolean disableCertificateValidation() default false;

    /**
     * RabbitMQ queue name.
     * @return RabbitMQ queue name.
     */
    String queueName() default "";
}
