package orders.service;

import java.util.List;
import java.util.ArrayList;
import java.util.Set;
import java.util.HashSet;
import java.util.stream.Collectors;
import java.util.logging.Logger;
import java.time.Instant;

import io.grpc.stub.StreamObserver;
import com.google.protobuf.Int32Value;
import com.google.protobuf.StringValue;
// import com.google.rpc.ErrorInfo;
// import com.google.rpc.Status;

import org.hibernate.Session;
import org.hibernate.query.Query;

import orders.hibernate.HibernateUtil;
import orders.hibernate.model.Address;
import orders.hibernate.model.Order;
import orders.hibernate.model.Product;

import jwtauth.*;
import orders.*;

public class OrdersImpl extends OrdersGrpc.OrdersImplBase {

    static String unauthorizedErrDescr = "Unauthorized";
    private static final Logger logger = Logger.getLogger(OrdersImpl.class.getName());

    @Override
    public void list(OrderListRequest req, StreamObserver<OrderListReply> responseObserver) {

        List<Integer> authorizedIds = authorizeRequest("GetOrder", responseObserver);
        if (authorizedIds.isEmpty()) {
            return;
        }

        Session session = HibernateUtil.getSession();

        Query<Order> query = session.createQuery(
                "from Order where user in (:user) and deletedBy is null and deletedAt is null order by id desc",
                Order.class);
        query.setParameter("user", authorizedIds);

        int count = req.getCount();
        if (count < 1)
            count = 10;

        int offset = req.getOffset();
        if (offset < 0)
            offset = 0;

        query.setFirstResult(offset);
        query.setMaxResults(count);

        List<Order> orders = query.list();

        OrderListReply.Builder reply = OrderListReply.newBuilder();
        orders.forEach(o -> reply.addOrdersList(getOrderReplyFromOrder(o)));

        Query<Long> countQuery = session.createQuery(
                "Select count(o.id) from Order o where user in (:user) and deletedBy is null and deletedAt is null",
                Long.class);
        countQuery.setParameter("user", authorizedIds);
        Long totalRecords = countQuery.getSingleResult();

        reply.setOffset(offset);
        reply.setCount(count);
        int total = totalRecords > Integer.MAX_VALUE ? Integer.MAX_VALUE : totalRecords.intValue();
        reply.setTotal(total);

        responseObserver.onNext(reply.build());
        responseObserver.onCompleted();
    }

    @Override
    public void get(OrderRequest.Id req, StreamObserver<OrderReply> responseObserver) {
        List<Integer> authorizedIds = authorizeRequest("GetOrder", responseObserver);
        if (authorizedIds.isEmpty()) {
            return;
        }

        Session session = HibernateUtil.getSession();

        Order order = findOrder(session, req.getId(), authorizedIds, responseObserver);

        OrderReply reply = getOrderReplyFromOrder(order);

        responseObserver.onNext(reply);
        responseObserver.onCompleted();
    }

    @Override
    public void create(OrderRequest req, StreamObserver<OrderReply> responseObserver) {
        List<Integer> authorizedIds = authorizeRequest("CreateOrder", responseObserver);
        if (authorizedIds.isEmpty()) {
            return;
        }

        AddressReply address = req.getAddress();

        if (null == address.getStreet() ||
                null == address.getZipCode() ||
                null == address.getCountryCode()) {
            throw new IllegalArgumentException();
        }

        AuthInfo authInfo = Constant.CLIENT_CONTEXT_KEY.get();

        Order order = getOrderFromRequest(req, authInfo.clientId);

        if (!authorizedIds.contains(order.getUser())) {
            responseObserver.onError(io.grpc.Status.PERMISSION_DENIED
                    .withDescription(unauthorizedErrDescr)
                    .asRuntimeException(null));
            return;
        }

        Session session = HibernateUtil.getSession();
        session.beginTransaction();
        session.persist(order);
        Set<Product> products = order.getProducts();
        products.forEach(session::refresh);
        session.getTransaction().commit();
        session.close();

        OrderReply reply = getOrderReplyFromOrder(order);

        responseObserver.onNext(reply);
        responseObserver.onCompleted();
    }

    @Override
    public void update(OrderRequest req, StreamObserver<OrderReply> responseObserver) {
        List<Integer> authorizedIds = authorizeRequest("UpdateOrder", responseObserver);
        if (authorizedIds.isEmpty()) {
            return;
        }

        Int32Value user = null;
        if (req.hasUser()) {
            user = req.getUser();
            if (!authorizedIds.contains(user.getValue())) {
                responseObserver.onError(io.grpc.Status.PERMISSION_DENIED
                        .withDescription(unauthorizedErrDescr)
                        .asRuntimeException(null));
                return;
            }
        }

        Session session = HibernateUtil.getSession();
        Order order = findOrder(session, req.getId(), authorizedIds, responseObserver);
        if (order == null) {
            return;
        }

        if (!authorizedIds.contains(order.getUser())) {
            responseObserver.onError(io.grpc.Status.PERMISSION_DENIED
                    .withDescription(unauthorizedErrDescr)
                    .asRuntimeException(null));
            return;
        }

        // Order user changed
        // It means that new order will be removed and created for other user
        if (null != user && order.getUser() != user.getValue()) {
            List<Integer> createAuthorizedIds = authorizeRequest("CreateOrder", responseObserver);
            List<Integer> deleteAuthorizedIds = authorizeRequest("DeleteOrder", responseObserver);
            if (!createAuthorizedIds.contains(user.getValue())
                    || !deleteAuthorizedIds.contains(order.getUser())) {
                responseObserver.onError(io.grpc.Status.PERMISSION_DENIED
                        .withDescription(unauthorizedErrDescr)
                        .asRuntimeException(null));
            }
        }

        if (null != user) {
            order.setUser(user.getValue());
        }

        order.setCustomer(req.getCustomer());

        if (req.hasComment()) {
            order.setComment(req.getComment().getValue());
        }

        Address address = order.getAddress();
        updateAddressFromRequest(address, req.getAddress());

        order.setUpdatedAt(Instant.now());
        order.setUpdatedBy(authorizedIds.get(0));

        List<ProductReply> newProducts = req.getOrderProductListList();
        Set<Integer> newIds = newProducts.stream().map(ProductReply::getId).collect(Collectors.toSet());
        List<Product> products = findProducts(session, newIds);
        if (products.size() != newProducts.size()) {
            Set<Integer> ids = products.stream().map(Product::getId).collect(Collectors.toSet());
            newIds.removeAll(ids);

            responseObserver.onError(io.grpc.Status.NOT_FOUND
                    .withDescription("Products with ids: " + newIds + " not found")
                    .asRuntimeException(null));
            return;
        }

        order.setProducts(products.stream().collect(Collectors.toSet()));

        session.beginTransaction();
        session.persist(order);
        session.getTransaction().commit();
        session.close();

        OrderReply reply = getOrderReplyFromOrder(order);
        responseObserver.onNext(reply);
        responseObserver.onCompleted();
    }

    @Override
    public void delete(OrderRequest.Id req, StreamObserver<OrderReply> responseObserver) {
        List<Integer> authorizedIds = authorizeRequest("DeleteOrder", responseObserver);
        if (authorizedIds.isEmpty()) {
            return;
        }

        Session session = HibernateUtil.getSession();
        Order order = getOrderById(session, req.getId());
        if (null == order) {
            responseObserver.onError(io.grpc.Status.NOT_FOUND
                    .withDescription("Order with id specified not found")
                    .asRuntimeException(null));
            return;
        } else if (!authorizedIds.contains(order.getUser())) {
            responseObserver.onError(io.grpc.Status.PERMISSION_DENIED
                    .withDescription(unauthorizedErrDescr)
                    .asRuntimeException(null));
            return;
        }

        order.setDeletedAt(Instant.now());
        order.setDeletedBy(authorizedIds.get(0));

        session.beginTransaction();
        session.persist(order);
        session.getTransaction().commit();

        OrderReply reply = getOrderReplyFromOrder(order);

        session.close();

        responseObserver.onNext(reply);
        responseObserver.onCompleted();
    }

    private static List<Integer> getUsersByRole(AuthInfo authInfo, String role) {
        ArrayList<Integer> l = new ArrayList<>();
        if (null != authInfo && null != authInfo.clientRoles) {
            for (String s : authInfo.clientRoles) {
                String[] parts = s.split("\\/");
                if (parts.length == 2 && parts[1].equals(role)) {
                    try {
                        l.add(Integer.parseInt(parts[0]));
                    } catch (Exception e) {
                        // Ignore this role
                    }
                } else if (s.equals(role)) {
                    l.add(authInfo.clientId);
                }
            }
        }

        return l;
    }

    private static <T> List<Integer> authorizeRequest(String role, StreamObserver<T> responseObserver) {
        AuthInfo authInfo = Constant.CLIENT_CONTEXT_KEY.get();
        List<Integer> adminIds = getUsersByRole(authInfo, "Admin");
        List<Integer> ids = getUsersByRole(authInfo, role);
        if (ids.isEmpty() && adminIds.isEmpty()) {
            logger.warning("Unauthorized " + role + " request");
            responseObserver.onError(io.grpc.Status.PERMISSION_DENIED
                    .withDescription(unauthorizedErrDescr)
                    .asRuntimeException(null));
        }

        ids.addAll(adminIds);
        return ids;
    }

    private static Order getUserOrderById(Session session, long id, List<Integer> user) {
        Query<Order> query = session.createQuery(
                "from Order where id = :id and user in (:user) and deletedBy is null and deletedAt is null",
                Order.class);
        query.setParameter("id", id);
        query.setParameter("user", user);
        return query.getSingleResultOrNull();
    }

    private static Order getOrderById(Session session, long id) {
        Query<Order> query = session.createQuery(
                "from Order where id = :id and deletedBy is null and deletedAt is null",
                Order.class);
        query.setParameter("id", id);
        return query.getSingleResultOrNull();
    }

    private static List<Product> findProducts(Session session, Set<Integer> ids) {
        Query<Product> query = session.createQuery("from Product where id in (:ids)", Product.class);
        query.setParameter("ids", ids);
        return query.list();
    }

    private static <T> Order findOrder(Session session, long id, List<Integer> user,
            StreamObserver<T> responseObserver) {
        Order order = getUserOrderById(session, id, user);
        if (null == order) {
            /*
             * Status status = Status.newBuilder()
             * .setCode(com.google.rpc.Code.NOT_FOUND.getNumber())
             * .setMessage("Order not found")
             * .addDetails(Any.pack(ErrorInfo.newBuilder()
             * .setReason("Invalid Id")
             * .setDomain("microservices.orders")
             * .putMetadata("id", String.valueOf(req.getId()))
             * .build()))
             * .build();
             * responseObserver.onError(StatusProto.toStatusRuntimeException(status));
             */
            // Metadata.Key<Empty> errorResponseKey =
            // ProtoUtils.keyForProto(Empty.getDefaultInstance());
            // Empty errorResponse = Empty.newBuilder().build();
            // Metadata metadata = new Metadata();
            // metadata.put(errorResponseKey, errorResponse);

            responseObserver.onError(io.grpc.Status.NOT_FOUND
                    .withDescription("Order with id specified not found")
                    .asRuntimeException(null));
        }
        return order;
    }

    private static Address getAddressFromRequest(AddressReply addressReply) {
        Address address = new Address();
        updateAddressFromRequest(address, addressReply);
        return address;
    }

    private static Address updateAddressFromRequest(Address address, AddressReply addressReply) {
        address.setCountryCode(addressReply.getCountryCode());
        address.setStreet(addressReply.getStreet());
        address.setZipCode(addressReply.getZipCode());
        return address;
    }

    private static Order getOrderFromRequest(OrderRequest req, int createdBy) {
        Order order = new Order();
        order.setCreatedAt(Instant.now());

        if (req.hasUser()) {
            order.setUser(req.getUser().getValue());
        } else {
            order.setUser(createdBy);
        }

        order.setCreatedBy(createdBy);

        if (req.hasComment()) {
            order.setComment(req.getComment().getValue());
        }

        order.setCustomer(req.getCustomer());

        HashSet<Order> orders = new HashSet<>();
        orders.add(order);

        AddressReply addressReply = req.getAddress();
        Address address = getAddressFromRequest(addressReply);
        address.setOrder(order);
        order.setAddress(address);

        List<ProductReply> products = req.getOrderProductListList();
        HashSet<Product> orderProducts = new HashSet<>();
        products.forEach(p -> {
            Product product = new Product();
            product.setId(p.getId());
            orderProducts.add(product);
        });

        order.setProducts(orderProducts);
        return order;
    }

    private static OrderReply getOrderReplyFromOrder(Order order) {
        if (order == null) {
            throw new IllegalArgumentException();
        }

        Address address = order.getAddress();
        AddressReply.Builder addressBuilder = AddressReply.newBuilder()
                .setId(address.getId())
                .setStreet(address.getStreet())
                .setZipCode(address.getZipCode())
                .setCountryCode(address.getCountryCode());

        OrderRequest.Builder orderReqBuilder = OrderRequest.newBuilder();
        orderReqBuilder.setId(order.getId())
                .setCustomer(order.getCustomer())
                .setAddress(addressBuilder);

        Integer user = order.getUser();
        if (null != user) {
            orderReqBuilder.setUser(Int32Value.newBuilder().setValue(user));
        }

        String comment = order.getComment();
        if (null != comment) {
            orderReqBuilder.setComment(StringValue.newBuilder().setValue(comment));
        }

        ProductReply.Builder productBuilder = ProductReply.newBuilder();
        order.getProducts().forEach(p -> {
            ProductReply product = productBuilder.clear()
                    .setId(p.getId())
                    .setName(p.getName())
                    .setDescription(p.getDescription())
                    .build();

            orderReqBuilder.addOrderProductList(product);
        });

        OrderReply.Builder orderBuilder = OrderReply.newBuilder();
        orderBuilder.setData(orderReqBuilder)
                .setCreatedBy(order.getCreatedBy())
                .setCreatedAt(order.getCreatedAt().toString());

        Integer updatedBy = order.getUpdatedBy();
        if (null != updatedBy) {
            orderBuilder.setUpdatedBy(Int32Value.newBuilder().setValue(updatedBy));
        }

        Instant updatedAt = order.getUpdatedAt();
        if (null != updatedAt) {
            orderBuilder.setUpdatedAt(StringValue.newBuilder().setValue(updatedAt.toString()));
        }

        Integer deletedBy = order.getDeletedBy();
        if (null != deletedBy) {
            orderBuilder.setDeletedBy(Int32Value.newBuilder().setValue(deletedBy));
        }

        Instant deletedAt = order.getDeletedAt();
        if (null != deletedAt) {
            orderBuilder.setDeletedAt(StringValue.newBuilder().setValue(deletedAt.toString()));
        }

        return orderBuilder.build();
    }
}