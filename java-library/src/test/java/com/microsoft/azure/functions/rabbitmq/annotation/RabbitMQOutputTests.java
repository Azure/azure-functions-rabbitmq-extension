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

        EasyMock.expect(outputMock.connectionStringSetting()).andReturn("testConnectionStringSetting");
        EasyMock.expect(outputMock.dataType()).andReturn("testDataType");
        EasyMock.expect(outputMock.disableCertificateValidation()).andReturn(true);
        EasyMock.expect(outputMock.queueName()).andReturn("testQueueName");
        EasyMock.replay(outputMock);

        Assert.assertEquals("testConnectionStringSetting", outputMock.connectionStringSetting());
        Assert.assertEquals("testDataType", outputMock.dataType());
        Assert.assertEquals(true, outputMock.disableCertificateValidation());
        Assert.assertEquals("testQueueName", outputMock.queueName());
    }
}
