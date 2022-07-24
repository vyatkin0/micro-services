package orders.hibernate.model;

import java.time.Instant;
import java.util.Set;

import jakarta.persistence.Entity;
import jakarta.persistence.Table;
import jakarta.persistence.Id;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.OneToOne;
import jakarta.persistence.ManyToMany;
import jakarta.persistence.Basic;
import jakarta.persistence.CascadeType;
import jakarta.persistence.Column;

@Entity
@Table( name = "orders" )
public class Order {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    private String comment;

    @Column(name="user_id", nullable = false)
    private int user;

    @Basic(optional=false)
    private int createdBy;

    @Basic(optional=false)
    private String customer;

    @Basic(optional=false)
    private Instant createdAt;

    private Integer updatedBy;
    private Instant updatedAt;

    private Integer deletedBy;
    private Instant deletedAt;

    @ManyToMany(cascade = CascadeType.DETACH)
    private Set<Product> products;

    @OneToOne(optional = false, cascade = CascadeType.ALL)
    private Address address;

    public Long getId() {
        return id;
    }

    public int getUser() {
        return user;
    }

    public void setUser(int id) {
        this.user = id;
    }

    public String getComment() {
        return comment;
    }

    public void setComment(String comment) {
        this.comment = comment;
    }

    public String getCustomer() {
        return customer;
    }

    public void setCustomer(String customer) {
        this.customer = customer;
    }

    public Address getAddress() {
        return address;
    }

    public void setAddress(Address address) {
        this.address = address;
    }

    public Set<Product> getProducts() {
        return products;
    }

    public void setProducts(Set<Product> orderProducts) {
        this.products = orderProducts;
    }

    public int getCreatedBy() {
        return createdBy;
    }

    public void setCreatedBy(Integer id) {
        this.createdBy = id;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(Instant date) {
        this.createdAt = date;
    }

    public Integer getDeletedBy() {
        return deletedBy;
    }

    public void setDeletedBy(Integer id) {
        this.deletedBy = id;
    }

    public Instant getDeletedAt() {
        return deletedAt;
    }

    public void setDeletedAt(Instant date) {
        this.deletedAt = date;
    }

    public Integer getUpdatedBy() {
        return updatedBy;
    }

    public void setUpdatedBy(Integer id) {
        this.updatedBy = id;
    }

    public Instant getUpdatedAt() {
        return updatedAt;
    }

    public void setUpdatedAt(Instant date) {
        this.updatedAt = date;
    }
}

