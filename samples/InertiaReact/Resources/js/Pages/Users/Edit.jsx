import React from 'react';
import { useForm, usePage } from '@inertiajs/react';

export default function Edit({ user }) {
    const { errors } = usePage().props;
    const { data, setData, put, processing } = useForm({
        name: user.name || '',
        email: user.email || '',
        role: user.role || 'User',
    });

    const submit = (e) => {
        e.preventDefault();
        put(`/users/${user.id}`);
    };

    return (
        <div className="page-users-edit">
            <h1>Edit User</h1>

            <div className="card">
                <form onSubmit={submit}>
                    <div className="form-group">
                        <label htmlFor="name">Name</label>
                        <input
                            type="text"
                            id="name"
                            value={data.name}
                            onChange={e => setData('name', e.target.value)}
                            className={errors.name ? 'error' : ''}
                        />
                        {errors.name && <div className="error-message">{errors.name}</div>}
                    </div>

                    <div className="form-group">
                        <label htmlFor="email">Email</label>
                        <input
                            type="email"
                            id="email"
                            value={data.email}
                            onChange={e => setData('email', e.target.value)}
                            className={errors.email ? 'error' : ''}
                        />
                        {errors.email && <div className="error-message">{errors.email}</div>}
                    </div>

                    <div className="form-group">
                        <label htmlFor="role">Role</label>
                        <select
                            id="role"
                            value={data.role}
                            onChange={e => setData('role', e.target.value)}
                        >
                            <option value="User">User</option>
                            <option value="Admin">Admin</option>
                        </select>
                    </div>

                    <div className="form-actions">
                        <button type="submit" disabled={processing} className="btn btn-primary">
                            Update User
                        </button>
                        <a href="/users" className="btn btn-secondary">Cancel</a>
                    </div>
                </form>
            </div>
        </div>
    );
}
