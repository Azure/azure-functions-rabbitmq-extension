/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */

package com.microsoft.azure.functions.rabbitmq;

/**
 * Secure Socket Layer (SSL) policy errors.
 */
public enum SslPolicyErrors {
    /**
     * No SSL policy errors.
     */
    NONE(0),

    /**
     * Certificate not available.
     */
    REMOTE_CERTIFICATE_NOT_AVAILABLE(1),

    /**
     * Certificate name mismatch.
     */
    REMOTE_CERTIFICATE_NAME_MISMATCH(2),

    /**
     * ChainStatus has returned a non empty array.
     */
    REMOTE_CERTIFICATE_CHAIN_ERRORS(4);

    private int value;

    SslPolicyErrors(final int value) {
        this.value = value;
    }

    public int value() {
        return this.value;
    }
}
