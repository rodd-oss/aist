Todo app for software developers and ai agents.

Actors:
- user via cli tool
- ai agent via cli tool

Workflow
```
- user asks manager agent to create new job for solving user needs
- manager agent creates job (feature, refactor, chore, fmt, fix, doc)
- manager agent converts the job to user stories with acceptence criterias@

- user asks developer agent to pull the job to complete it
- developer agent pulls the latest job with user stories and acceptence criterias
- when job is about mutating the code it is always separate git branch named {job_id}_{job_short_slug}
- while acceptence criteria not met
  - developer agent explores the codebase to complete the most priority story
  - developer agent plans how to complete the story
  - developer agent completes the story
- developer agent creates pull request
- developer agent marks job as done
```


Database structure:
```
project {
  id (unique)
  title
  created_at
}

job {
  id (unique)
  project_id (reference to project)
  short_slug
  title
  status (todo, in_progress, done)
  description
  created_at
}

user_stories {
  id (unique)
  job_id
  title
  who
  what
  why
  priority
  created_at
}

user_stories_acceptance_criterias {
  id (unique)
  user_story_id (reference to user_story)
  description
  is_met
  created_at
}

user_story_progress_log {
  id (unique)
  task_id (reference to task)
  text
  created_at
}
```
