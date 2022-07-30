package jwtauth;

import io.grpc.Context;
import io.grpc.Metadata;

import static io.grpc.Metadata.ASCII_STRING_MARSHALLER;

/**
 * Constants definition
 */
public final class Constant {
    static final String JWT_SIGNING_KEY = "1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
    static final String BEARER_TYPE = "Bearer";
    static final String CLAIM_MS_ROLE = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    static final Metadata.Key<String> AUTHORIZATION_METADATA_KEY = Metadata.Key.of("Authorization", ASCII_STRING_MARSHALLER);
    public static final Context.Key<AuthInfo> CLIENT_CONTEXT_KEY = Context.key("client");

    private Constant() {
        throw new AssertionError();
    }
}
