package orders.service;

import java.util.List;

import orders.*;
import org.hibernate.Session;
import org.hibernate.query.Query;

import orders.hibernate.HibernateUtil;
import orders.hibernate.model.Product;

import io.grpc.stub.StreamObserver;

import com.google.protobuf.Empty;
public class ProductsImpl extends orders.ProductsGrpc.ProductsImplBase {
    @Override
    public void list(Empty request, StreamObserver<orders.ProductListReply> responseObserver) {
        Session session = HibernateUtil.getSession();

        Query<Product> query = session.createQuery("from Product order by name", Product.class);
        List<Product> products = query.list();

        ProductListReply.Builder reply = ProductListReply.newBuilder();
        ProductReply.Builder productBuilder = ProductReply.newBuilder();
        products.forEach(p -> {
            ProductReply product = getProductReplyFromProduct(productBuilder, p);
            reply.addProductList(product);
        });

        responseObserver.onNext(reply.build());
        responseObserver.onCompleted();
    }

    @Override
    public void get(orders.ProductRequest.Id request, StreamObserver<orders.ProductReply> responseObserver) {

        Session session = HibernateUtil.getSession();
        Query<Product> query = session.createQuery("from Product where id = :id", Product.class);
        query.setParameter("id", request.getId());

        Product p = query.getSingleResultOrNull();

        ProductReply.Builder productBuilder = ProductReply.newBuilder();
        responseObserver.onNext(getProductReplyFromProduct(productBuilder, p));
        responseObserver.onCompleted();
    }

    private static ProductReply getProductReplyFromProduct(ProductReply.Builder productBuilder, Product product) {
        return productBuilder.clear()
                .setId(product.getId())
                .setName(product.getName())
                .setDescription(product.getDescription())
                .build();
    }
}