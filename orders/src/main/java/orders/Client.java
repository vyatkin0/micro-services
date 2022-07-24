/*
 * Copyright 2015 The gRPC Authors
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

package orders;

import io.grpc.Channel;
import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;
import io.grpc.StatusRuntimeException;

import java.util.concurrent.TimeUnit;
import java.util.logging.Level;
import java.util.logging.Logger;

import com.google.protobuf.StringValue;

/**
 * A simple client that requests a greeting from the {@link App}.
 */
public class Client {
  private static final Logger logger = Logger.getLogger(Client.class.getName());

  private final OrdersGrpc.OrdersBlockingStub blockingStub;

  /** Construct client for accessing HelloWorld server using the existing channel. */
  public Client(Channel channel) {
    // 'channel' here is a Channel, not a ManagedChannel, so it is not this code's responsibility to
    // shut it down.

    // Passing Channels to code makes code easier to test and makes it easier to reuse Channels.
    blockingStub = OrdersGrpc.newBlockingStub(channel);
  }

  public void getOrder(long Id) {
    logger.info("Will try to getOrder " + Id + " ...");
    try {
      OrderReply response = blockingStub.get(OrderRequest.Id.newBuilder().setId(Id).build());
      logger.info("getOrder: " + response.getData().getComment());
      logger.info("getOrder: " + response.getData().getCustomer());

    } catch (StatusRuntimeException e) {
      logger.log(Level.WARNING, "RPC failed: {0}", e.getStatus());
    }
  }

  public void createOrder() {
    logger.info("Will try to createOrder ...");
    try {
      OrderReply response = blockingStub.create(createOrderRequest());
      logger.info("getOrder: " + response.getData().getComment());
      logger.info("getOrder: " + response.getData().getCustomer());

    } catch (StatusRuntimeException e) {
      logger.log(Level.WARNING, "RPC failed: {0}", e.getStatus());
    }
  }

  OrderRequest createOrderRequest() {
    OrderRequest.Builder orderBuilder = OrderRequest.newBuilder();

    AddressReply.Builder addressBuilder = AddressReply.newBuilder()
        .setStreet("street2")
        .setZipCode("zip2")
        .setCountryCode("RU");

    orderBuilder
        .setCustomer("customer2")
        .setComment(StringValue.newBuilder().setValue("comment"))
        .setAddress(addressBuilder);

    ProductReply.Builder productBuilder = ProductReply.newBuilder();
    ProductReply product1 = productBuilder.clear().setId(1).build();
    ProductReply product2 = productBuilder.clear().setId(2).build();
    orderBuilder.addOrderProductList(product1);
    orderBuilder.addOrderProductList(product2);

    return orderBuilder.build();
  }

  /**
   * Greet server. If provided, the first element of {@code args} is the name to use in the
   * greeting. The second argument is the target server.
   */
  public static void main(String[] args) throws Exception {
    String user = "world";
    // Access a service running on the local machine on port 50051
    String target = "localhost:5103";
    // Allow passing in the user and target strings as command line arguments
    if (args.length > 0) {
      if ("--help".equals(args[0])) {
        System.err.println("Usage: [name [target]]");
        System.err.println("");
        System.err.println("  name    The name you wish to be greeted by. Defaults to " + user);
        System.err.println("  target  The server to connect to. Defaults to " + target);
        System.exit(1);
      }
      user = args[0];
    }
    if (args.length > 1) {
      target = args[1];
    }

    // Create a communication channel to the server, known as a Channel. Channels are thread-safe
    // and reusable. It is common to create channels at the beginning of your application and reuse
    // them until the application shuts down.
    ManagedChannel channel = ManagedChannelBuilder.forTarget(target)
        // Channels are secure by default (via SSL/TLS). For the example we disable TLS to avoid
        // needing certificates.
        .usePlaintext()
        .build();
    try {
      Client client = new Client(channel);
      client.createOrder();
      client.getOrder(1);
    } finally {
      // ManagedChannels use resources like threads and TCP connections. To prevent leaking these
      // resources the channel should be shut down when it will no longer be used. If it may be used
      // again leave it running.
      channel.shutdownNow().awaitTermination(5, TimeUnit.SECONDS);
    }
  }
}
