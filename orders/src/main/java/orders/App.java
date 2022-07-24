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

import io.grpc.Server;
import io.grpc.ServerBuilder;
import jwtauth.JwtServerInterceptor;

import java.io.IOException;
import java.util.concurrent.TimeUnit;
import java.util.logging.Logger;

import orders.service.ProductsImpl;
import orders.service.OrdersImpl;

/**
 * Server that manages startup/shutdown of a {@code Greeter} server.
 */
public class App {
  private static final Logger logger = Logger.getLogger(App.class.getName());

  private Server server;

  private void start() throws IOException {
    /* The port on which the server should run */
    int port = 5103;
    server = ServerBuilder.forPort(port)
        .addService(new OrdersImpl())
        .addService(new ProductsImpl())
        .intercept(new JwtServerInterceptor())  // add the JwtServerInterceptor
        .build()
        .start();
    logger.info("Server started, listening on " + port);
    Runtime.getRuntime().addShutdownHook(new Thread() {
      @Override
      public void run() {
        // Use stderr here since the logger may have been reset by its JVM shutdown hook.
        logger.info("*** shutting down gRPC server since JVM is shutting down");
        try {
          App.this.stop();
        } catch (InterruptedException e) {
          e.printStackTrace(System.err);
        }
        logger.info("*** server shut down");
      }
    });
  }

  private void stop() throws InterruptedException {
    if (server != null) {
      server.shutdown().awaitTermination(30, TimeUnit.SECONDS);
    }
  }

  /**
   * Await termination on the main thread since the grpc library uses daemon threads.
   */
  private void blockUntilShutdown() throws InterruptedException {
    if (server != null) {
      server.awaitTermination();
    }
  }

  /**
   * Main launches the server from the command line.
   */
  public static void main(String[] args) throws IOException, InterruptedException {
    final App server = new App();
    server.start();
    server.blockUntilShutdown();

    /*
    Session session = HibernateUtil.getSession();
    Transaction tx = null;

    try {
      tx = session.beginTransaction();
      // do some work

      List result = session.createQuery( "from Order" ).list();
      System.out.println(result.size());
      for ( Order order : (List<Order>) result ) {
          System.out.println( "Order (" + order.getId() + ") : " + order.getComment() );
      }

      tx.commit();
    }

    catch (Exception e) {
      if (tx!=null) tx.rollback();
      e.printStackTrace(); 
    } finally {
      session.close();
    }

    */
  }
}
