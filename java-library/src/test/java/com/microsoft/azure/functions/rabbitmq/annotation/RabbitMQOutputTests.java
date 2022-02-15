/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.rabbitmq.annotation;

import org.easymock.*;
import org.junit.*;

import com.microsoft.azure.functions.rabbitmq.SslPolicyErrors;

public class RabbitMQOutputTests {

    @Test
    public void TestRabbitMQOutput() {
        RabbitMQOutput outputMock = EasyMock.mock(RabbitMQOutput.class);

        EasyMock.expect(outputMock.queueName()).andReturn("dummyQueueName");
        EasyMock.expect(outputMock.connectionStringSetting()).andReturn("dummyConnectionStringSetting");

        SslPolicyErrors[] sslPolicyErrors = new SslPolicyErrors[] { SslPolicyErrors.REMOTE_CERTIFICATE_NAME_MISMATCH,
                SslPolicyErrors.REMOTE_CERTIFICATE_CHAIN_ERRORS };

        EasyMock.expect(outputMock.acceptablePolicyErrors()).andReturn(sslPolicyErrors);
        EasyMock.replay(outputMock);

        Assert.assertEquals("dummyQueueName", outputMock.queueName());
        Assert.assertEquals("dummyConnectionStringSetting", outputMock.connectionStringSetting());

        SslPolicyErrors[] outSslPolicyErrors = outputMock.acceptablePolicyErrors();
        Assert.assertEquals(2, outSslPolicyErrors.length);
        Assert.assertEquals(SslPolicyErrors.REMOTE_CERTIFICATE_NAME_MISMATCH, outSslPolicyErrors[0]);
        Assert.assertEquals(SslPolicyErrors.REMOTE_CERTIFICATE_CHAIN_ERRORS, outSslPolicyErrors[1]);
    }
}
