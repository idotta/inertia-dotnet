import React, { useState } from 'react';
import { Link, router } from '@inertiajs/react';

export default function Index({ users, stats }) {
    const [showStats, setShowStats] = useState(false);

    const loadStats = () => {
        // Demonstrate partial reload - only load the stats prop
        router.reload({ only: ['stats'] });
        setShowStats(true);
    };

    const deleteUser = (id) => {
        if (confirm('Are you sure you want to delete this user?')) {
            router.delete(`/users/${id}`);
        }
    };

    return (
        <div className="page-users-index">
            <div className="page-header">
                <h1>Users</h1>
                <Link href="/users/create" className="btn btn-primary">
                    Create User
                </Link>
            </div>

            {!showStats && stats === undefined && (
                <button onClick={loadStats} className="btn btn-secondary mb-3">
                    Load Statistics (Optional Prop Demo)
                </button>
            )}

            {stats && (
                <div className="card mb-3">
                    <h3>Statistics</h3>
                    <p>Total Users: {stats.totalUsers}</p>
                    <p>Active Today: {stats.activeToday}</p>
                </div>
            )}

            <div className="table-container">
                <table>
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Name</th>
                            <th>Email</th>
                            <th>Role</th>
                            <th>Created</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map(user => (
                            <tr key={user.id}>
                                <td>{user.id}</td>
                                <td>{user.name}</td>
                                <td>{user.email}</td>
                                <td>{user.role}</td>
                                <td>{new Date(user.createdAt).toLocaleDateString()}</td>
                                <td>
                                    <Link href={`/users/${user.id}`} className="btn btn-sm">
                                        View
                                    </Link>
                                    <Link href={`/users/${user.id}/edit`} className="btn btn-sm">
                                        Edit
                                    </Link>
                                    <button
                                        onClick={() => deleteUser(user.id)}
                                        className="btn btn-sm btn-danger"
                                    >
                                        Delete
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}
