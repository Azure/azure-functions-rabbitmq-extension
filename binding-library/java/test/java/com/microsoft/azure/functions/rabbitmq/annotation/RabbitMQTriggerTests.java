/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
 
package com.microsoft.azure.functions.rabbitmq.annotation;

import org.junit.*;
import org.easymock.*;

public class RabbitMQTriggerTests {

    @Test
    public void TestRabbitMQTrigger() {
        RabbitMQTrigger triggerInterface = EasyMock.mock(RabbitMQTrigger.class);

        EasyMock.expect(triggerInterface.hostName()).andReturn("randomHostName");
        EasyMock.expect(triggerInterface.passwordSetting()).andReturn("randomPassword");
        EasyMock.expect(triggerInterface.queueName()).andReturn("randomQueueName");
        EasyMock.expect(triggerInterface.userNameSetting()).andReturn("randomUserName");
        EasyMock.expect(triggerInterface.virtualHost()).andReturn("randomVirtualHost");
        EasyMock.expect(triggerInterface.connectionStringSetting()).andReturn("randomConnectionStringSetting");
        EasyMock.expect(triggerInterface.port()).andReturn(123);

        EasyMock.replay(triggerInterface);
    }

}
