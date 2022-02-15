/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.rabbitmq.annotation;

import org.easymock.*;
import org.junit.*;

import com.microsoft.azure.functions.rabbitmq.SslPolicyErrors;

public class RabbitMQTriggerTests {

    @Test
    public void TestRabbitMQTrigger() {
        RabbitMQTrigger triggerMock = EasyMock.mock(RabbitMQTrigger.class);

        EasyMock.expect(triggerMock.queueName()).andReturn("dummyQueueName");
        EasyMock.expect(triggerMock.connectionStringSetting()).andReturn("dummyConnectionStringSetting");

        SslPolicyErrors[] sslPolicyErrors = new SslPolicyErrors[] { SslPolicyErrors.REMOTE_CERTIFICATE_NAME_MISMATCH,
                SslPolicyErrors.REMOTE_CERTIFICATE_CHAIN_ERRORS };

        EasyMock.expect(triggerMock.acceptablePolicyErrors()).andReturn(sslPolicyErrors);
        EasyMock.replay(triggerMock);

        Assert.assertEquals("dummyQueueName", triggerMock.queueName());
        Assert.assertEquals("dummyConnectionStringSetting", triggerMock.connectionStringSetting());

        SslPolicyErrors[] outSslPolicyErrors = triggerMock.acceptablePolicyErrors();
        Assert.assertEquals(2, outSslPolicyErrors.length);
        Assert.assertEquals(SslPolicyErrors.REMOTE_CERTIFICATE_NAME_MISMATCH, outSslPolicyErrors[0]);
        Assert.assertEquals(SslPolicyErrors.REMOTE_CERTIFICATE_CHAIN_ERRORS, outSslPolicyErrors[1]);
    }
}
