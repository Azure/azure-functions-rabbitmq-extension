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

        EasyMock.expect(triggerMock.queueName()).andReturn("dummyQueueName");
        EasyMock.expect(triggerMock.connectionStringSetting()).andReturn("dummyConnectionStringSetting");
        EasyMock.expect(triggerMock.disableCertificateValidation()).andReturn(true);
        EasyMock.expect(triggerMock.dataType()).andReturn("string");
        EasyMock.replay(triggerMock);

        Assert.assertEquals("dummyQueueName", triggerMock.queueName());
        Assert.assertEquals("dummyConnectionStringSetting", triggerMock.connectionStringSetting());
        Assert.assertEquals(true, triggerMock.disableCertificateValidation());
        Assert.assertEquals("string", triggerMock.dataType());
    }
}
