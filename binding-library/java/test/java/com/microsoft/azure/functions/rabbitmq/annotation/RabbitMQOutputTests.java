/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
 
package com.microsoft.azure.functions.rabbitmq.annotation;

import org.junit.*;
import org.easymock.*;

public class RabbitMQOutputTests {

    @Test
    public void TestRabbitMQOutput() {
        RabbitMQOutput outputInterface = EasyMock.mock(RabbitMQOutput.class);

        EasyMock.expect(outputInterface.hostName()).andReturn("randomHostName");
        EasyMock.expect(outputInterface.password()).andReturn("randomPassword");
        EasyMock.expect(outputInterface.queueName()).andReturn("randomQueueName");
        EasyMock.expect(outputInterface.userName()).andReturn("randomUserName");
        EasyMock.expect(outputInterface.virtualHost()).andReturn("randomVirtualHost");
        EasyMock.expect(outputInterface.connectionStringSetting()).andReturn("randomConnectionStringSetting");
        EasyMock.expect(outputInterface.port()).andReturn(123);

        EasyMock.replay(outputInterface);
    }

}
