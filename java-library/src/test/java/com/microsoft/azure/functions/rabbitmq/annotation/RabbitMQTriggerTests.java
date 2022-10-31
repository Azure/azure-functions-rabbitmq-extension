/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.rabbitmq.annotation;

import org.easymock.*;
import org.junit.*;

public class RabbitMQTriggerTests {

    @Test
    public void TestRabbitMQTrigger() {
        RabbitMQTrigger triggerMock = EasyMock.mock(RabbitMQTrigger.class);

        EasyMock.expect(triggerMock.connectionStringSetting()).andReturn("testConnectionStringSetting");
        EasyMock.expect(triggerMock.dataType()).andReturn("testDataType");
        EasyMock.expect(triggerMock.disableCertificateValidation()).andReturn(true);
        EasyMock.expect(triggerMock.queueName()).andReturn("testQueueName");
        EasyMock.replay(triggerMock);

        Assert.assertEquals("testConnectionStringSetting", triggerMock.connectionStringSetting());
        Assert.assertEquals("testDataType", triggerMock.dataType());
        Assert.assertEquals(true, triggerMock.disableCertificateValidation());
        Assert.assertEquals("testQueueName", triggerMock.queueName());
    }
}
