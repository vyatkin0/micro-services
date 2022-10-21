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
  }
}
