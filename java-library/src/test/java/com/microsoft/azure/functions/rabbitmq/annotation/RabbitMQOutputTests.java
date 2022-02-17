/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.rabbitmq.annotation;

import org.easymock.*;
import org.junit.*;

public class RabbitMQOutputTests {

    @Test
    public void TestRabbitMQOutput() {
        RabbitMQOutput outputMock = EasyMock.mock(RabbitMQOutput.class);

        EasyMock.expect(outputMock.queueName()).andReturn("dummyQueueName");
        EasyMock.expect(outputMock.connectionStringSetting()).andReturn("dummyConnectionStringSetting");
        EasyMock.expect(outputMock.disableCertificateValidation()).andReturn(true);
        EasyMock.replay(outputMock);

        Assert.assertEquals("dummyQueueName", outputMock.queueName());
        Assert.assertEquals("dummyConnectionStringSetting", outputMock.connectionStringSetting());
        Assert.assertEquals(true, outputMock.disableCertificateValidation());
    }
}
