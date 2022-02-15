/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.rabbitmq.annotation;

import com.microsoft.azure.functions.annotation.CustomBinding;
import com.microsoft.azure.functions.rabbitmq.SslPolicyErrors;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * <p>
 * Java annotation for RabbitMQ trigger binding. The type of parameter can be a
 * native Java type such as <code>int</code>, <code>String</code> or
 * <code>byte[]</code> or a POJO.
 * </p>
 *
 * <p>
 * Example function that uses a RabbitMQ trigger:
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
     */
    String connectionStringSetting() default "";

    /**
     * RabbitMQ queue name.
     */
    String queueName() default "";

    /**
     * Array of TLS policy (peer verification) errors that are deemed acceptable.
     */
    SslPolicyErrors[] acceptablePolicyErrors() default { SslPolicyErrors.NONE };
}
