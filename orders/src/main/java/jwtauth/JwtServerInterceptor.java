/*
 * Copyright 2019 The gRPC Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package jwtauth;

import java.io.UnsupportedEncodingException;
import java.util.ArrayList;

import io.grpc.Context;
import io.grpc.Contexts;
import io.grpc.Metadata;
import io.grpc.ServerCall;
import io.grpc.ServerCallHandler;
import io.grpc.ServerInterceptor;
import io.grpc.Status;
import io.jsonwebtoken.Claims;
import io.jsonwebtoken.Jws;
import io.jsonwebtoken.JwtException;
import io.jsonwebtoken.JwtParser;
import io.jsonwebtoken.Jwts;

/**
 * This interceptor gets the JWT from the metadata, verifies it and sets the
 * client identifier
 * obtained from the token into the context. In order not to complicate the
 * example with additional
 * checks (expiration date, issuer and etc.), it relies only on the signature of
 * the token for
 * verification.
 */
public class JwtServerInterceptor implements ServerInterceptor {
  @Override
  public <ReqT, RespT> ServerCall.Listener<ReqT> interceptCall(ServerCall<ReqT, RespT> serverCall, Metadata metadata,
      ServerCallHandler<ReqT, RespT> serverCallHandler) {
    String value = metadata.get(Constant.AUTHORIZATION_METADATA_KEY);

    Status status = Status.OK;
    if (value == null) {
      Context ctx = Context.current().withValue(Constant.CLIENT_CONTEXT_KEY, null);
      return Contexts.interceptCall(ctx, serverCall, metadata, serverCallHandler);
      // status = Status.UNAUTHENTICATED.withDescription("Authorization token is
      // missing");
    } else if (!value.startsWith(Constant.BEARER_TYPE)) {
      status = Status.UNAUTHENTICATED.withDescription("Unknown authorization type");
    } else {
      Jws<Claims> claims = null;
      // remove authorization type prefix
      String token = value.substring(Constant.BEARER_TYPE.length()).trim();
      try {
        JwtParser parser = Jwts.parser().setSigningKey(Constant.JWT_SIGNING_KEY.getBytes("UTF8"));
        // verify token signature and parse claims
        claims = parser
            .requireIssuer("https://github.com/vyatkin0/micro-services")
            .requireAudience("https://github.com/vyatkin0/micro-services")
            .parseClaimsJws(token);
      } catch (JwtException e) {
        status = Status.UNAUTHENTICATED.withDescription(e.getMessage()).withCause(e);
      } catch (UnsupportedEncodingException e) {
        status = Status.UNAUTHENTICATED.withDescription(e.getMessage());
      }

      if (claims != null) {
        AuthInfo info = new AuthInfo();
        info.clientId = Integer.parseInt(claims.getBody().getSubject());

        Object roles = claims.getBody().getOrDefault(Constant.CLAIM_MS_ROLE, new String[0]);
        if (roles instanceof String) {
          info.clientRoles = new String[] { (String) roles };
        } else {
          info.clientRoles = roles instanceof ArrayList<?> ? ((ArrayList<?>) roles).toArray(new String[0]) : new String[0];
        }

        Context ctx = Context.current()
            .withValue(Constant.CLIENT_CONTEXT_KEY, info);
        return Contexts.interceptCall(ctx, serverCall, metadata, serverCallHandler);
      }
    }

    serverCall.close(status, new Metadata());
    return new ServerCall.Listener<ReqT>() {
      // noop
    };
  }
}
