import React, { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AdminFormLayout } from '../../../../components/admin/AdminFormLayout';
import { Button, Field, inputClasses } from '../../../../components/ui';
import { teamMembersApi } from '../../../../lib/api/team-members';
import type { CreateTeamMemberRequest } from '../../../../lib/api/types';

const TeamMemberCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const formRef = useRef<HTMLFormElement>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<CreateTeamMemberRequest>({
    name: '',
    position: '',
    positionEn: '',
    region: 'National',
    regionEn: 'National',
    zone: '',
    zoneEn: '',
    photo: '',
    bio: '',
    bioEn: '',
    email: '',
    order: 0,
    isActive: true
  });

  const backPath = '/admin/team-members';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      const response = await teamMembersApi.createTeamMember(formData);
      if (response.success && response.data) {
        navigate(`/admin/team-members/${response.data.id}`);
      } else {
        setError('Failed to create team member');
      }
    } catch (err) {
      console.error('Error creating team member:', err);
      setError('Error creating team member');
    } finally {
      setSubmitting(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'number' ? parseInt(value) :
              type === 'checkbox' ? (e.target as HTMLInputElement).checked :
              value
    }));
  };

  return (
    <form ref={formRef} onSubmit={handleSubmit} className="min-w-0">
      <AdminFormLayout
        title="Add New Team Member"
        backPath={backPath}
        backLabel="Cancel"
        onCancel={() => navigate(backPath)}
        onSave={() => formRef.current?.requestSubmit()}
        actions={
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? 'Creating...' : 'Create Member'}
          </Button>
        }
        main={
          <div>
            {error && (
              <p className="mb-6 border border-error bg-surface px-4 py-3 text-error">{error}</p>
            )}
            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              <div className="md:col-span-2">
                <Field label="Name" htmlFor="name" required>
                  <input
                    type="text"
                    id="name"
                    name="name"
                    value={formData.name}
                    onChange={handleChange}
                    required
                    className={inputClasses}
                    placeholder="Full name"
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label="Position" htmlFor="position" required>
                  <input
                    type="text"
                    id="position"
                    name="position"
                    value={formData.position}
                    onChange={handleChange}
                    required
                    className={inputClasses}
                    placeholder="e.g., Co-Président"
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label="Position (English)" htmlFor="positionEn">
                  <input type="text" id="positionEn" name="positionEn" value={formData.positionEn} onChange={handleChange} className={inputClasses} placeholder="e.g., Co-Chair" />
                </Field>
              </div>

              <Field label="Region" htmlFor="region" required>
                <input
                  type="text"
                  id="region"
                  name="region"
                  value={formData.region}
                  onChange={handleChange}
                  required
                  className={inputClasses}
                />
              </Field>

              <Field label="Region (English)" htmlFor="regionEn">
                <input type="text" id="regionEn" name="regionEn" value={formData.regionEn} onChange={handleChange} className={inputClasses} />
              </Field>

              <Field label="Zone" htmlFor="zone" required>
                <select
                  id="zone"
                  name="zone"
                  value={formData.zone}
                  onChange={handleChange}
                  required
                  className={`${inputClasses} cursor-pointer`}
                >
                  <option value="">Select Zone</option>
                  <option value="Zone 1">Zone 1</option>
                  <option value="Zone 2">Zone 2</option>
                </select>
              </Field>

              <Field label="Zone (English)" htmlFor="zoneEn">
                <input type="text" id="zoneEn" name="zoneEn" value={formData.zoneEn} onChange={handleChange} className={inputClasses} placeholder="Zone 1" />
              </Field>

              <div className="md:col-span-2">
                <Field label="Email" htmlFor="email">
                  <input
                    type="email"
                    id="email"
                    name="email"
                    value={formData.email}
                    onChange={handleChange}
                    className={inputClasses}
                    placeholder="email@example.com"
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label="Photo URL" htmlFor="photo">
                  <input
                    type="url"
                    id="photo"
                    name="photo"
                    value={formData.photo}
                    onChange={handleChange}
                    className={inputClasses}
                    placeholder="https://example.com/photo.jpg"
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label="Biography" htmlFor="bio">
                  <textarea
                    id="bio"
                    name="bio"
                    value={formData.bio}
                    onChange={handleChange}
                    rows={4}
                    className={inputClasses}
                    placeholder="Brief biography..."
                  />
                </Field>
              </div>

              <div className="md:col-span-2">
                <Field label="Biography (English)" htmlFor="bioEn">
                  <textarea id="bioEn" name="bioEn" value={formData.bioEn} onChange={handleChange} rows={4} className={inputClasses} placeholder="Brief biography in English..." />
                </Field>
              </div>

              <Field label="Display Order" htmlFor="order" required hint="Lower numbers appear first">
                <input
                  type="number"
                  id="order"
                  name="order"
                  value={formData.order}
                  onChange={handleChange}
                  required
                  min="0"
                  className={inputClasses}
                />
              </Field>

              <div className="flex items-center">
                <label htmlFor="isActive" className="flex min-h-[44px] cursor-pointer items-center gap-3">
                  <input
                    type="checkbox"
                    id="isActive"
                    name="isActive"
                    checked={formData.isActive}
                    onChange={handleChange}
                    className="h-5 w-5 rounded-control-sm border border-outline accent-green"
                  />
                  <span className="text-body-md text-ink">Active</span>
                </label>
              </div>
            </div>
          </div>
        }
      />
    </form>
  );
};

export default TeamMemberCreatePage;
