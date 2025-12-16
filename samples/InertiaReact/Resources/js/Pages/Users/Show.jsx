import React from 'react';
import { Link } from '@inertiajs/react';

export default function Show({ user, timestamp }) {
    return (
        <div className="page-users-show">
            <div className="page-header">
                <h1>User Details</h1>
                <Link href="/users" className="btn btn-secondary">
                    Back to Users
                </Link>
            </div>

            <div className="card">
                <div className="user-details">
                    <div className="detail-row">
                        <strong>ID:</strong>
                        <span>{user.id}</span>
                    </div>
                    <div className="detail-row">
                        <strong>Name:</strong>
                        <span>{user.name}</span>
                    </div>
                    <div className="detail-row">
                        <strong>Email:</strong>
                        <span>{user.email}</span>
                    </div>
                    <div className="detail-row">
                        <strong>Role:</strong>
                        <span>{user.role}</span>
                    </div>
                    <div className="detail-row">
                        <strong>Created At:</strong>
                        <span>{new Date(user.createdAt).toLocaleString()}</span>
                    </div>
                    <div className="detail-row">
                        <strong>Page Loaded At:</strong>
                        <span>{new Date(timestamp).toLocaleString()}</span>
                        <em className="text-muted">(Always Prop - updates on every reload)</em>
                    </div>
                </div>

                <div className="form-actions">
                    <Link href={`/users/${user.id}/edit`} className="btn btn-primary">
                        Edit User
                    </Link>
                </div>
            </div>
        </div>
    );
}
