# Kubernetes Deployment Guide for Mordecai MUD

This guide explains how to deploy Mordecai MUD to Kubernetes, including configuration for RabbitMQ and SQLite database persistence.

## Overview

Mordecai MUD is now configured to support Kubernetes deployment with the following features:

- **Configurable RabbitMQ Connection**: Supports both connection strings (Aspire) and individual parameters (Kubernetes)
- **Persistent Database Storage**: SQLite database path configurable via environment variables for persistent volumes
- **Aspire Integration**: Leverages .NET Aspire for local development and manifest generation

## Configuration Architecture

### RabbitMQ Configuration

The application supports two configuration methods:

#### 1. Connection String (Aspire/Local Development)
```json
{
  "ConnectionStrings": {
    "messaging": "amqp://username:password@hostname:5672/virtualhost"
  }
}
```

#### 2. Individual Parameters (Kubernetes/Production)
```json
{
  "RabbitMQ": {
    "Host": "rabbitmq-service",
    "Port": 5672,
    "Username": "gameuser",
    "Password": "secretpassword",
    "VirtualHost": "/"
  }
}
```

**Environment Variable Override Priority:**
1. `RABBITMQ_HOST` > `RabbitMQ:Host`
2. `RABBITMQ_PORT` > `RabbitMQ:Port`
3. `RABBITMQ_USERNAME` > `RabbitMQ:Username`
4. `RABBITMQ_PASSWORD` > `RabbitMQ:Password`
5. `RABBITMQ_VIRTUALHOST` > `RabbitMQ:VirtualHost`

### Database Configuration

The SQLite database path can be configured via:

**Configuration Priority:**
1. `DATABASE_PATH` environment variable (highest priority)
2. `DatabasePath` configuration value
3. `ConnectionStrings:DefaultConnection` (extracts path from connection string)
4. Default: `mordecai.db` in application directory

**Example:**
```bash
DATABASE_PATH=/data/mordecai/mordecai.db
```

## Kubernetes Deployment

### Prerequisites

- Kubernetes cluster (1.25+)
- kubectl configured
- Persistent volume provisioner available
- RabbitMQ deployment or external RabbitMQ service

### Step 1: Create Namespace

```yaml
# namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: mordecai-mud
```

```bash
kubectl apply -f namespace.yaml
```

### Step 2: Create Secrets

Store sensitive configuration in Kubernetes secrets:

```yaml
# secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: mordecai-secrets
  namespace: mordecai-mud
type: Opaque
stringData:
  rabbitmq-username: gameuser
  rabbitmq-password: your-secure-password
```

```bash
kubectl apply -f secrets.yaml
```

### Step 3: Create Persistent Volume Claim

```yaml
# pvc.yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: mordecai-db-pvc
  namespace: mordecai-mud
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
  storageClassName: standard  # Adjust based on your cluster
```

```bash
kubectl apply -f pvc.yaml
```

### Step 4: Deploy RabbitMQ (if not using external service)

```yaml
# rabbitmq-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: rabbitmq
  namespace: mordecai-mud
spec:
  replicas: 1
  selector:
    matchLabels:
      app: rabbitmq
  template:
    metadata:
      labels:
        app: rabbitmq
    spec:
      containers:
      - name: rabbitmq
        image: rabbitmq:3.13-management
        ports:
        - containerPort: 5672
          name: amqp
        - containerPort: 15672
          name: management
        env:
        - name: RABBITMQ_DEFAULT_USER
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: rabbitmq-username
        - name: RABBITMQ_DEFAULT_PASS
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: rabbitmq-password
---
apiVersion: v1
kind: Service
metadata:
  name: rabbitmq-service
  namespace: mordecai-mud
spec:
  selector:
    app: rabbitmq
  ports:
  - name: amqp
    port: 5672
    targetPort: 5672
  - name: management
    port: 15672
    targetPort: 15672
```

```bash
kubectl apply -f rabbitmq-deployment.yaml
```

### Step 5: Deploy Mordecai Web Application

```yaml
# mordecai-web-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mordecai-web
  namespace: mordecai-mud
spec:
  replicas: 2  # Scale based on player load
  selector:
    matchLabels:
      app: mordecai-web
  template:
    metadata:
      labels:
        app: mordecai-web
    spec:
      containers:
      - name: mordecai-web
        image: your-registry/mordecai-web:latest
        ports:
        - containerPort: 8080
          name: http
        env:
        # Database Configuration
        - name: DATABASE_PATH
          value: "/data/mordecai.db"
        
        # RabbitMQ Configuration
        - name: RABBITMQ_HOST
          value: "rabbitmq-service"
        - name: RABBITMQ_PORT
          value: "5672"
        - name: RABBITMQ_USERNAME
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: rabbitmq-username
        - name: RABBITMQ_PASSWORD
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: rabbitmq-password
        - name: RABBITMQ_VIRTUALHOST
          value: "/"
        
        # ASP.NET Core Configuration
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        
        volumeMounts:
        - name: db-storage
          mountPath: /data
        
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "2000m"
        
        livenessProbe:
          httpGet:
            path: /alive
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
      
      volumes:
      - name: db-storage
        persistentVolumeClaim:
          claimName: mordecai-db-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: mordecai-web-service
  namespace: mordecai-mud
spec:
  selector:
    app: mordecai-web
  ports:
  - name: http
    port: 80
    targetPort: 8080
  type: LoadBalancer  # Or ClusterIP with Ingress
```

```bash
kubectl apply -f mordecai-web-deployment.yaml
```

### Step 6: Configure Ingress (Optional)

```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: mordecai-ingress
  namespace: mordecai-mud
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/websocket-services: mordecai-web-service
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - mordecai.yourdomain.com
    secretName: mordecai-tls
  rules:
  - host: mordecai.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: mordecai-web-service
            port:
              number: 80
```

```bash
kubectl apply -f ingress.yaml
```

## Using Aspire for Manifest Generation

.NET Aspire can generate Kubernetes manifests for you:

### Generate Manifests

```bash
# From the Mordecai.AppHost project
dotnet run --project Mordecai.AppHost --publisher manifest --output-path ./manifests
```

This generates deployment manifests that you can customize further.

## Environment Variables Reference

### Required Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `DATABASE_PATH` | Absolute path to SQLite database file | `/data/mordecai.db` |
| `RABBITMQ_HOST` | RabbitMQ hostname or service name | `rabbitmq-service` |
| `RABBITMQ_PORT` | RabbitMQ AMQP port | `5672` |
| `RABBITMQ_USERNAME` | RabbitMQ username | `gameuser` |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `secretpassword` |

### Optional Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `RABBITMQ_VIRTUALHOST` | RabbitMQ virtual host | `/` |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Production` |
| `ASPNETCORE_URLS` | URLs to listen on | `http://+:8080` |

## Persistent Volume Considerations

### SQLite Database Persistence

**Important Notes:**
- SQLite requires `ReadWriteOnce` access mode
- Only one pod can access the database at a time
- For multi-replica deployments, consider:
  - Using `ReadWriteMany` with a shared filesystem (if supported)
  - Migrating to PostgreSQL for true multi-instance support
  - Implementing leader election for database access

### Migration to PostgreSQL

For production scaling with multiple replicas, consider migrating to PostgreSQL:

1. Update connection string format
2. Replace `UseSqlite` with `UseNpgsql` in `Program.cs`
3. Deploy PostgreSQL StatefulSet or use managed service
4. Update environment variables:
   ```bash
   DATABASE_CONNECTION_STRING="Host=postgres-service;Database=mordecai;Username=dbuser;Password=dbpassword"
   ```

## Monitoring and Observability

### OpenTelemetry Integration

The application uses OpenTelemetry for distributed tracing:

```yaml
# Add to deployment environment variables
- name: OTEL_EXPORTER_OTLP_ENDPOINT
  value: "http://otel-collector:4317"
- name: OTEL_SERVICE_NAME
  value: "mordecai-web"
```

### Health Checks

- **Liveness**: `/alive` - Basic application health
- **Readiness**: `/health` - Includes dependency checks (RabbitMQ, Database)

## Scaling Considerations

### Horizontal Pod Autoscaling

```yaml
# hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: mordecai-web-hpa
  namespace: mordecai-mud
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: mordecai-web
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

**Note:** With SQLite, only one replica can write to the database. For true horizontal scaling, migrate to PostgreSQL.

### RabbitMQ Scaling

Consider RabbitMQ clustering for high availability:

```yaml
# Use RabbitMQ cluster operator or StatefulSet
# See: https://www.rabbitmq.com/kubernetes/operator/operator-overview.html
```

## Security Best Practices

### 1. Use Kubernetes Secrets
- Store all credentials in secrets
- Never commit secrets to version control
- Use external secret management (e.g., Azure Key Vault, AWS Secrets Manager)

### 2. Network Policies
```yaml
# network-policy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: mordecai-network-policy
  namespace: mordecai-mud
spec:
  podSelector:
    matchLabels:
      app: mordecai-web
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 8080
  egress:
  - to:
    - podSelector:
        matchLabels:
          app: rabbitmq
    ports:
    - protocol: TCP
      port: 5672
```

### 3. Pod Security Standards
```yaml
# Add to deployment spec
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 1000
  seccompProfile:
    type: RuntimeDefault
containers:
- name: mordecai-web
  securityContext:
    allowPrivilegeEscalation: false
    capabilities:
      drop:
      - ALL
    readOnlyRootFilesystem: true
```

## Troubleshooting

### Database Connection Issues

```bash
# Check database file permissions
kubectl exec -it -n mordecai-mud <pod-name> -- ls -la /data/

# Check database path configuration
kubectl exec -it -n mordecai-mud <pod-name> -- env | grep DATABASE

# Verify persistent volume mount
kubectl describe pvc -n mordecai-mud mordecai-db-pvc
```

### RabbitMQ Connection Issues

```bash
# Check RabbitMQ service DNS
kubectl exec -it -n mordecai-mud <pod-name> -- nslookup rabbitmq-service

# Test RabbitMQ connection
kubectl exec -it -n mordecai-mud <pod-name> -- curl rabbitmq-service:15672

# Check RabbitMQ logs
kubectl logs -n mordecai-mud -l app=rabbitmq

# Verify environment variables
kubectl exec -it -n mordecai-mud <pod-name> -- env | grep RABBITMQ
```

### Application Logs

```bash
# View application logs
kubectl logs -n mordecai-mud -l app=mordecai-web --tail=100 -f

# Check for startup errors
kubectl logs -n mordecai-mud <pod-name> --previous
```

## Backup and Recovery

### Database Backup

```bash
# Create backup job
kubectl create job -n mordecai-mud db-backup-$(date +%Y%m%d) \
  --from=cronjob/mordecai-db-backup

# Manual backup
kubectl exec -n mordecai-mud <pod-name> -- \
  sqlite3 /data/mordecai.db ".backup '/data/backup-$(date +%Y%m%d).db'"

# Copy backup locally
kubectl cp mordecai-mud/<pod-name>:/data/backup-$(date +%Y%m%d).db \
  ./backup-$(date +%Y%m%d).db
```

### Automated Backup CronJob

```yaml
# backup-cronjob.yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: mordecai-db-backup
  namespace: mordecai-mud
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: alpine:latest
            command:
            - sh
            - -c
            - |
              apk add --no-cache sqlite
              BACKUP_FILE="/backups/mordecai-$(date +%Y%m%d-%H%M%S).db"
              sqlite3 /data/mordecai.db ".backup '$BACKUP_FILE'"
              echo "Backup created: $BACKUP_FILE"
              # Cleanup old backups (keep last 7 days)
              find /backups -name "mordecai-*.db" -mtime +7 -delete
            volumeMounts:
            - name: db-storage
              mountPath: /data
            - name: backup-storage
              mountPath: /backups
          restartPolicy: OnFailure
          volumes:
          - name: db-storage
            persistentVolumeClaim:
              claimName: mordecai-db-pvc
          - name: backup-storage
            persistentVolumeClaim:
              claimName: mordecai-backup-pvc
```

## Development vs Production

### Local Development (Aspire)

```bash
# Start Aspire AppHost (includes RabbitMQ container)
dotnet run --project Mordecai.AppHost
```

Aspire automatically:
- Starts RabbitMQ in a container
- Configures connection strings
- Provides dashboard at http://localhost:15888

### Production Deployment

Use explicit environment variables for all configuration:
- No automatic service discovery
- Explicit connection parameters
- Persistent storage for database
- External RabbitMQ service or cluster

## Future Considerations

### Migration to PostgreSQL

When scaling beyond 10-50 concurrent users or requiring true multi-instance support:

1. **Update Data Project**: Add `Npgsql.EntityFrameworkCore.PostgreSQL` package
2. **Update Program.cs**: Replace `UseSqlite` with `UseNpgsql`
3. **Update Configuration**: Use PostgreSQL connection strings
4. **Deploy PostgreSQL**: Use StatefulSet or managed service (Azure Database, AWS RDS, etc.)
5. **Migrate Data**: Export from SQLite and import to PostgreSQL

### Service Mesh Integration

For advanced traffic management and observability, consider integrating with:
- Istio
- Linkerd
- Consul

### GitOps Deployment

Implement continuous deployment with:
- ArgoCD
- Flux
- GitHub Actions + Helm

## Support and Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [RabbitMQ on Kubernetes](https://www.rabbitmq.com/kubernetes/operator/operator-overview.html)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)

---

**Last Updated:** 2025-01-23  
**Version:** 1.0  
**Maintainer:** Mordecai MUD Development Team
