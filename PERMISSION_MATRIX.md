# EchoSpace Permission Matrix

## User Roles
- **User (Role 0)**: Regular user with basic permissions
- **Operation (Role 1)**: Content moderation role with limited admin access
- **Admin (Role 2)**: Highest privilege - full system access

## Permission Matrix

| Action | User | Operation | Admin |
|--------|------|-----------|-------|
| **Post Management** |
| Create Post | ✓ | ✓ | ✓ |
| Edit Own Post | ✓ | ✓ | ✓ |
| Delete Own Post | ✓ | ✓ | ✓ |
| Block/Unblock Post | ✗ | ✓ | ✓ |
| Delete Any Post | ✗ | ✓ | ✓ |
| Review Post Content | ✗ | ✓ | ✓ |
| View All Posts | ✗ | ✓ (moderation view) | ✓ |
| **User Management** |
| Create User | ✗ | ✗ | ✓ |
| Delete User | ✗ | ✗ | ✓ |
| Suspend User | ✗ | ✓ (temporary) | ✓ |
| Change User Role | ✗ | ✗ | ✓ |
| Lock/Unlock User Account | ✗ | ✗ | ✓ |
| View User List | ✗ | ✗ | ✓ |
| **Dashboard & Analytics** |
| View Dashboard (Read-Only) | ✗ | ✓ (limited) | ✓ |
| View Analytics | ✗ | ✓ (limited) | ✓ |
| View User Growth Charts | ✗ | ✓ | ✓ |
| View Post Activity Charts | ✗ | ✓ | ✓ |
| View Login Activity Charts | ✗ | ✓ | ✓ |
| View Active Sessions | ✗ | ✓ (view only) | ✓ |
| Terminate Sessions | ✗ | ✗ | ✓ |
| View Failed Login Attempts | ✗ | ✓ | ✓ |
| **System Administration** |
| System Settings | ✗ | ✗ | ✓ |
| Manage Tags | ✗ | ✗ | ✓ |
| View Audit Logs | ✗ | ✓ (moderation only) | ✓ |
| **Content Moderation** |
| Moderate Posts | ✗ | ✓ | ✓ |
| Moderate Comments | ✗ | ✓ | ✓ |

## Access Details

### Dashboard Access
- **Admin**: Full access - can view all data and perform actions (terminate sessions, etc.)
- **Operation**: Read-only access - can view all analytics, charts, and data but cannot perform edit actions
- **User**: No access

### User Management
- **Admin**: Full access - can create, delete, lock/unlock users, and change roles
- **Operation**: No access
- **User**: No access

### Post Management
- **Admin**: Full access - can delete any post, block/unblock posts
- **Operation**: Can delete any post, block/unblock posts, review content
- **User**: Can only manage own posts

## Notes
- Operation users can view the dashboard but cannot terminate sessions or perform other administrative actions
- Operation users can temporarily suspend users but cannot delete them or change roles
- All users can manage their own content (posts, comments, profile)

